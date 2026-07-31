using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void CorrelatedCompositeRangeSample_WhenCompiled_ShouldUseTypedPartitionIndex()
    {
        var result = CompileSampleForInspection(CorrelatedCompositeRangeMarkSampleFileName);

        Assert.Contains("PredicateRangeMark", result.PlanningText);
        Assert.Contains("PhysicalSortMergeJoin [LeftMark]", result.PhysicalPlanText);
        Assert.Contains("[partitions:", result.PhysicalPlanText);
        Assert.Contains("CreateRangeIndex", result.ExecutionPlanText);
        Assert.Contains("RangeProbe", result.ExecutionPlanText);
        Assert.Contains("ValueTuple<string, string>?", result.GeneratedCSharpCode);
        Assert.DoesNotContain("CreateAsOfEqualityKey", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(SmartForEachPattern, StringComparison.Ordinal));
    }

    [TestMethod]
    public void CorrelatedCompositeRangeSample_WhenCheckedIn_ShouldRetainTypedHotPath()
    {
        var sample = ReadSample(CorrelatedCompositeRangeMarkSampleFileName);

        Assert.Contains("PhysicalSortMergeJoin [LeftMark]", sample.Content);
        Assert.Contains("ValueTuple<string, string>?", sample.Content);
        Assert.Contains("CreateRangeJoinIndex<", sample.Content);
        Assert.IsFalse(sample.Content.Contains("CreateAsOfEqualityKey", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CorrelatedCompositeRangeSample_WhenCompiledForExecution_ShouldRun()
    {
        var table = CompileSampleForExecution(CorrelatedCompositeRangeMarkSampleFileName).Run();

        Assert.IsNotNull(table);
    }
}
