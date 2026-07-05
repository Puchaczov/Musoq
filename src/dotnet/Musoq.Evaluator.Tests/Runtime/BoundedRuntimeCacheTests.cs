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
}
