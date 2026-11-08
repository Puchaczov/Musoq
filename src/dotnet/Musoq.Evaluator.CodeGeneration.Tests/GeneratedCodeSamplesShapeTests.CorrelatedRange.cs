using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void CorrelatedCompositeRangeSample_WhenCompiled_ShouldUseDemandDrivenTypedCaptures()
    {
        var result = CompileSampleForInspection(CorrelatedCompositeRangeMarkSampleFileName);

        Assert.Contains("-> Apply", result.PlanningText);
        Assert.Contains("string __outer0, string __outer1, decimal __outer2", result.GeneratedCSharpCode);
        Assert.DoesNotContain("CreateAsOfEqualityKey", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(SmartForEachPattern, StringComparison.Ordinal));
    }

    [TestMethod]
    public void CorrelatedCompositeRangeSample_WhenCheckedIn_ShouldRetainDemandDrivenTypedCaptures()
    {
        var sample = ReadSample(CorrelatedCompositeRangeMarkSampleFileName);

        Assert.Contains("ApplyExists[Subquery0]", sample.Content);
        Assert.Contains("string __outer0, string __outer1, decimal __outer2", sample.Content);
        Assert.Contains("SubqueryExecution.Exists<", sample.Content);
        Assert.IsFalse(sample.Content.Contains("CreateAsOfEqualityKey", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CorrelatedCompositeRangeSample_WhenCompiledForExecution_ShouldRun()
    {
        var table = CompileSampleForExecution(CorrelatedCompositeRangeMarkSampleFileName).Run();

        Assert.IsNotNull(table);
    }
}
