using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class BoundedRuntimeCacheTests
{
    [TestMethod]
    public void GetOrAdd_WhenKeyAlreadyExists_ShouldReuseValue()
    {
        var cache = new BoundedRuntimeCache<string, int>(2, StringComparer.Ordinal);
        var factoryCalls = 0;

        var first = cache.GetOrAdd("a", _ => ++factoryCalls);
        var second = cache.GetOrAdd("a", _ => ++factoryCalls);

        Assert.AreEqual(1, first);
        Assert.AreEqual(1, second);
        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public void GetOrAdd_WhenCacheIsFull_ShouldEvictOldestEntry()
    {
        var cache = new BoundedRuntimeCache<string, int>(2, StringComparer.Ordinal);

        cache.GetOrAdd("a", _ => 1);
        cache.GetOrAdd("b", _ => 2);
        cache.GetOrAdd("c", _ => 3);

        Assert.AreEqual(2, cache.Count);
        Assert.IsFalse(cache.TryGetValue("a", out _));
        Assert.IsTrue(cache.TryGetValue("b", out var second));
        Assert.IsTrue(cache.TryGetValue("c", out var third));
        Assert.AreEqual(2, second);
        Assert.AreEqual(3, third);
    }

    [TestMethod]
    public void Clear_ShouldRemoveAllEntries()
    {
        var cache = new BoundedRuntimeCache<string, int>(2, StringComparer.Ordinal);
        cache.GetOrAdd("a", _ => 1);

        cache.Clear();

        Assert.AreEqual(0, cache.Count);
        Assert.IsFalse(cache.TryGetValue("a", out _));
    }

    [TestMethod]
    public async Task GetOrAdd_WhenCalledConcurrentlyForSameKey_ShouldCreateValueOnce()
    {
        using var contention = new SynchronousContention();
        var cache = new BoundedRuntimeCache<string, int>(8, StringComparer.Ordinal)
        {
            EnterMutationGate = contention.EnterMonitor,
        };
        var factoryCalls = 0;
        var results = await contention.RunAsync(
            () =>
                cache.GetOrAdd(
                    "shared",
                    _ =>
                    {
                        var value = Interlocked.Increment(ref factoryCalls);
                        contention.HoldOwner();
                        return value;
                    }
                ),
            32
        );

        CollectionAssert.AreEqual(Enumerable.Repeat(1, 32).ToArray(), results);
        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public async Task GetOrAdd_WhenAnotherKeyFactoryIsBlocked_ShouldServeCachedValueWithoutTakingMutationLock()
    {
        var cache = new BoundedRuntimeCache<string, int>(8, StringComparer.Ordinal);
        cache.GetOrAdd("cached", static _ => 7);
        using var watchdog = new TestWatchdog();
        var factoryEntered = TestWatchdog.Signal();
        var releaseFactory = TestWatchdog.Signal();
        var blockedMiss = TestWatchdog.StartWorker(() =>
            cache.GetOrAdd(
                "blocked",
                _ =>
                {
                    factoryEntered.SetResult();
                    releaseFactory.Task.WaitAsync(watchdog.Token).GetAwaiter().GetResult();
                    return 11;
                }
            )
        );
        Task<int>? cachedHit = null;

        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await watchdog.ReachedBeforeCompletionAsync(factoryEntered.Task, blockedMiss, "the miss factory");
                cachedHit = TestWatchdog.StartWorker(() =>
                    cache.GetOrAdd("cached", static _ => throw new AssertFailedException())
                );
                await watchdog.WaitAsync(cachedHit, "the cache hit while the miss factory is held");
                Assert.AreEqual(7, await cachedHit);
            },
            () =>
            {
                releaseFactory.TrySetResult();
                return Task.WhenAll(blockedMiss, cachedHit ?? Task.CompletedTask);
            }
        );
    }

    [TestMethod]
    public async Task GetOrAdd_WhenExistingValueIsStale_ShouldReplaceOnceAndPreserveFifoPosition()
    {
        using var contention = new SynchronousContention();
        var cache = new BoundedRuntimeCache<string, int>(2, StringComparer.Ordinal)
        {
            EnterMutationGate = contention.EnterMonitor,
        };
        cache.GetOrAdd("a", static _ => 1);
        cache.GetOrAdd("b", static _ => 2);
        var replacementCalls = 0;
        var results = await contention.RunAsync(
            () =>
                cache.GetOrAdd(
                    "a",
                    _ =>
                    {
                        var value = Interlocked.Increment(ref replacementCalls) + 1;
                        contention.HoldOwner();
                        return value;
                    },
                    static value => value > 1
                ),
            32
        );
        CollectionAssert.AreEqual(Enumerable.Repeat(2, 32).ToArray(), results);
        cache.GetOrAdd("c", static _ => 3);

        Assert.AreEqual(1, replacementCalls);
        Assert.IsFalse(cache.TryGetValue("a", out _), "Replacing an entry must not make it newest.");
        Assert.IsTrue(cache.TryGetValue("b", out _));
        Assert.IsTrue(cache.TryGetValue("c", out _));
    }

    [TestMethod]
    public void TryGetValue_WhenReadersAndWritersRunConcurrently_ShouldRemainConsistent()
    {
        var cache = new BoundedRuntimeCache<int, int>(32);
        cache.GetOrAdd(0, static key => key);

        Parallel.For(
            0,
            256,
            index =>
            {
                cache.GetOrAdd(index % 64, static key => key);
                cache.TryGetValue(index % 64, out _);
            }
        );

        Assert.IsTrue(cache.Count <= 32);
        cache.GetOrAdd(63, static key => key);
        Assert.IsTrue(cache.TryGetValue(63, out var value));
        Assert.AreEqual(63, value);
    }

    [TestMethod]
    public void TypeKeyedCache_ShouldEvictStrongCollectibleLikeKeys()
    {
        var cache = new BoundedRuntimeCache<Type, string>(2);

        cache.GetOrAdd(typeof(string), static _ => "string");
        cache.GetOrAdd(typeof(int), static _ => "int");
        cache.GetOrAdd(typeof(Guid), static _ => "guid");

        Assert.AreEqual(2, cache.Count);
        Assert.IsFalse(cache.TryGetValue(typeof(string), out _));
        Assert.IsTrue(cache.TryGetValue(typeof(int), out _));
        Assert.IsTrue(cache.TryGetValue(typeof(Guid), out _));
    }
}
