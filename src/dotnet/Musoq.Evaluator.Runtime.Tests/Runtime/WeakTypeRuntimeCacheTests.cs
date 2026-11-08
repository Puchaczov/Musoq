using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class WeakTypeRuntimeCacheTests
{
    [TestMethod]
    public void Cache_ShouldEvictInInsertionOrder()
    {
        var cache = new WeakTypeRuntimeCache<string>(1);

        cache.GetOrAdd(typeof(int), static _ => "int");
        cache.GetOrAdd(typeof(string), static _ => "string");

        Assert.IsFalse(cache.TryGetValue(typeof(int), out _));
        Assert.IsTrue(cache.TryGetValue(typeof(string), out var value));
        Assert.AreEqual("string", value);
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public void Cache_ShouldCreateOneValueWhenAccessedConcurrently()
    {
        var cache = new WeakTypeRuntimeCache<string>(8);
        var factoryCalls = 0;

        Parallel.For(0, 32, _ => cache.GetOrAdd(
            typeof(WeakTypeRuntimeCacheTests),
            _ =>
            {
                Interlocked.Increment(ref factoryCalls);
                return "value";
            }));

        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public void Cache_ShouldReadExistingValueConcurrentlyWithoutChangingIt()
    {
        var cache = new WeakTypeRuntimeCache<string>(8);
        cache.GetOrAdd(typeof(WeakTypeRuntimeCacheTests), static _ => "value");

        Parallel.For(0, 256, _ =>
        {
            Assert.IsTrue(cache.TryGetValue(typeof(WeakTypeRuntimeCacheTests), out var value));
            Assert.AreEqual("value", value);
        });

        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public void Cache_Clear_ShouldReleaseAllEntries()
    {
        var cache = new WeakTypeRuntimeCache<string>(8);
        cache.GetOrAdd(typeof(int), static _ => "int");

        cache.Clear();

        Assert.AreEqual(0, cache.Count);
        Assert.IsFalse(cache.TryGetValue(typeof(int), out _));
    }

}
