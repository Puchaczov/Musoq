using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RegexMatchBudgetTests
{
    [TestMethod]
    public void DefaultBudget_ShouldBeTwoSeconds() =>
        Assert.AreEqual(TimeSpan.FromSeconds(2), new CompilationOptions().RegexMatchTimeout);

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-10000L)]
    [DataRow(9999L)]
    [DataRow(21474836460001L)]
    public void InvalidBudget_ShouldFailBeforeCompilation(long ticks) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompilationOptions().WithRegexMatchTimeout(TimeSpan.FromTicks(ticks)));

    [TestMethod]
    [DataRow(10000L)]
    [DataRow(21474836460000L)]
    public void SupportedBoundaryBudget_ShouldBePreserved(long ticks)
    {
        var options = new CompilationOptions().WithRegexMatchTimeout(TimeSpan.FromTicks(ticks));

        Assert.AreEqual(ticks, options.RegexMatchTimeout.Ticks);
    }

    [TestMethod]
    public void CloningOptions_ShouldRetainCustomBudget()
    {
        var options = new CompilationOptions().WithRegexMatchTimeout(TimeSpan.FromMilliseconds(321));

        var clone = options.WithInstrumentationMode(QueryInstrumentationMode.Full)
            .WithTableResultMaterialization().WithLoopInvariantCodeMotion(false)
            .WithStabilityAwareScalarReuse(false).WithRecursiveCteLimits(new(2, 3));

        Assert.AreEqual(options.RegexMatchTimeout, clone.RegexMatchTimeout);
    }

    [TestMethod]
    public void Fingerprint_WhenBudgetChanges_ShouldSeparateCompilationCaches()
    {
        var first = new CompilationOptions().WithRegexMatchTimeout(TimeSpan.FromSeconds(1));
        var second = first.WithRegexMatchTimeout(TimeSpan.FromSeconds(3));

        Assert.AreNotEqual(CompilationOptionsFingerprint.Compute(first), CompilationOptionsFingerprint.Compute(second));
        Assert.AreEqual(CompilationOptionsFingerprint.Compute(first),
            CompilationOptionsFingerprint.Compute(first.WithTableResultMaterialization(false)));
    }

    [TestMethod]
    public void PreparedLike_WhenBudgetsDiffer_ShouldNotReuseCachedMatcher()
    {
        var first = Operators.PrepareLike("Ż_łć", TimeSpan.FromSeconds(1))!;
        var second = Operators.PrepareLike("Ż_łć", TimeSpan.FromSeconds(3))!;
        var repeated = Operators.PrepareLike("Ż_łć", TimeSpan.FromSeconds(1))!;

        Assert.AreNotSame(first, second);
        Assert.AreSame(first, repeated);
        Assert.AreEqual(TimeSpan.FromSeconds(1), first.MatchTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(3), second.MatchTimeout);
        Assert.IsTrue(first.IsMatch("Żółć"));
        Assert.IsTrue(second.IsMatch("Żółć"));
    }

    [TestMethod]
    public async Task ConcurrentRLike_WhenBudgetsDiffer_ShouldNotReuseRegexAsync()
    {
        var first = Operators.PrepareRLike("v.lue", TimeSpan.FromSeconds(1))!;
        var second = Operators.PrepareRLike("v.lue", TimeSpan.FromSeconds(3))!;

        var results = await Task.WhenAll(Task.Run(() => first.IsMatch("value")), Task.Run(() => second.IsMatch("value")));

        CollectionAssert.AreEqual(new[] { true, true }, results);
        Assert.AreNotSame(first.ResolvedRegex, second.ResolvedRegex);
        Assert.AreEqual(TimeSpan.FromSeconds(1), first.ResolvedRegex!.MatchTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(3), second.ResolvedRegex!.MatchTimeout);
    }
}
