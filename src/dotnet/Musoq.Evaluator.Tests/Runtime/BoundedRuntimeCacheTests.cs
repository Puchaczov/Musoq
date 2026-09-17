using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;

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
    public void GetOrAdd_WhenCalledConcurrentlyForSameKey_ShouldCreateValueOnce()
    {
        var cache = new BoundedRuntimeCache<string, int>(8, StringComparer.Ordinal);
        var factoryCalls = 0;

        Parallel.For(0, 32, _ =>
        {
            cache.GetOrAdd("shared", _ => Interlocked.Increment(ref factoryCalls));
        });

        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public async Task GetOrAdd_WhenAnotherKeyFactoryIsBlocked_ShouldServeCachedValueWithoutTakingMutationLock()
    {
        var cache = new BoundedRuntimeCache<string, int>(8, StringComparer.Ordinal);
        cache.GetOrAdd("cached", static _ => 7);
        using var factoryEntered = new ManualResetEventSlim();
        using var releaseFactory = new ManualResetEventSlim();
        var blockedMiss = Task.Run(() => cache.GetOrAdd("blocked", _ =>
        {
            factoryEntered.Set();
            releaseFactory.Wait();
            return 11;
        }));

        Assert.IsTrue(factoryEntered.Wait(TimeSpan.FromSeconds(5)), "The cache miss factory did not start.");
        var cachedHit = Task.Run(() => cache.GetOrAdd("cached", static _ => throw new AssertFailedException()));

        try
        {
            Assert.AreEqual(7, await cachedHit.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            releaseFactory.Set();
            await blockedMiss.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [TestMethod]
    public void GetOrAdd_WhenExistingValueIsStale_ShouldReplaceOnceAndPreserveFifoPosition()
    {
        var cache = new BoundedRuntimeCache<string, int>(2, StringComparer.Ordinal);
        cache.GetOrAdd("a", static _ => 1);
        cache.GetOrAdd("b", static _ => 2);
        var replacementCalls = 0;

        Parallel.For(0, 32, _ =>
        {
            cache.GetOrAdd(
                "a",
                _ => Interlocked.Increment(ref replacementCalls) + 1,
                static value => value > 1);
        });
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

        Parallel.For(0, 256, index =>
        {
            cache.GetOrAdd(index % 64, static key => key);
            cache.TryGetValue(index % 64, out _);
        });

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
