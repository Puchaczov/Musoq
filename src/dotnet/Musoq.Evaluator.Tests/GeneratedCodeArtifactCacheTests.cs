using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeArtifactCacheTests
{
    [TestMethod]
    public async Task ConcurrentFirstCreation_ShouldInvokeFactoryOnce()
    {
        var cache = new GeneratedCodeArtifactCache<string, string>();
        var factoryCalls = 0;

        var values = await Task.WhenAll(
            Enumerable.Range(0, 32).Select(index => Task.Run(() =>
                cache.GetOrAdd(
                    "sample",
                    key =>
                    {
                        Interlocked.Increment(ref factoryCalls);
                        Thread.SpinWait(100_000);
                        return "generated";
                    },
                    out _))));

        Assert.AreEqual(1, factoryCalls);
        CollectionAssert.AreEqual(new[] { "generated" }, values.Distinct().ToArray());
    }

    [TestMethod]
    public void SuccessfulLookup_ShouldReportCacheHit()
    {
        var cache = new GeneratedCodeArtifactCache<string, string>();

        var first = cache.GetOrAdd("sample", _ => "generated", out var firstHit);
        var second = cache.GetOrAdd("sample", _ => "different", out var secondHit);

        Assert.AreEqual("generated", first);
        Assert.AreEqual("generated", second);
        Assert.IsFalse(firstHit);
        Assert.IsTrue(secondHit);
    }

    [TestMethod]
    public void FailedCreation_ShouldBeRetryable()
    {
        var cache = new GeneratedCodeArtifactCache<string, string>();
        var factoryCalls = 0;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            cache.GetOrAdd(
                "sample",
                _ =>
                {
                    factoryCalls++;
                    throw new InvalidOperationException("expected");
                },
                out _));

        var value = cache.GetOrAdd(
            "sample",
            _ =>
            {
                factoryCalls++;
                return "recovered";
            },
            out var cacheHit);

        Assert.AreEqual("recovered", value);
        Assert.IsFalse(cacheHit);
        Assert.AreEqual(2, factoryCalls);
    }
}
