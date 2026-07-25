using System;
using System.Reflection;
using System.Reflection.Emit;
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
    public void Cache_ShouldNotStronglyRetainCollectibleTypeKeys()
    {
        var cache = new WeakTypeRuntimeCache<string>(8);
        var weakType = PopulateCollectibleType(cache);

        ForceCollection(weakType);

        Assert.IsFalse(weakType.IsAlive);
        Assert.AreEqual(0, cache.Count);
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

    private static WeakReference PopulateCollectibleType(WeakTypeRuntimeCache<string> cache)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"Musoq.CollectibleCacheTest.{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule("main");
        var type = module.DefineType("CollectibleRow").CreateType()!;
        cache.GetOrAdd(type, static candidate => candidate.FullName ?? candidate.Name);
        return new WeakReference(type);
    }

    private static void ForceCollection(WeakReference weakReference)
    {
        for (var attempt = 0; attempt < 8 && weakReference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
