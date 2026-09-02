using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeSamplesScalarReuseShapeTests
{
    [TestMethod]
    [DataRow("Q252_StableFilterProjectionReuse.cs", "Population")]
    [DataRow("Q254_SharedStableWindowInputs.cs", "resultWindowRows")]
    [DataRow("Q256_ParallelAggregateSharedArguments.cs", "ParallelSingleKeyAggregate")]
    [DataRow("Q257_PivotPredicateDispatch.cs", "ResultAggregateGroup")]
    [DataRow("Q260_StableAsOfProbeKeys.cs", "CreateAsOfIndex")]
    [DataRow("Q261_StableRangeJoinKeys.cs", "CreateRangeJoinIndex")]
    [DataRow("Q262_CorrelatedCteProbeReuse.cs", "bHash")]
    [DataRow("Q263_StableUnpivotExpansion.cs", "__unpivot")]
    [DataRow("Q264_BoundaryRowShapeNarrowing.cs", "BoundedTopRecordList")]
    [DataRow("Q265_SourceComputedProjectionAccepted.cs", "Population * 2")]
    [DataRow("Q266_SourceComputedProjectionResidual.cs", "Population * 2")]
    [DataRow("Q267_RecursiveStableScalarInvariant.cs", "cte0CurrentFrontier")]
    public void ScalarReuseSample_ShouldExposeItsBoundaryShape(string fileName, string expectedToken)
    {
        var generated = ReadSample(fileName);
        StringAssert.Contains(generated, expectedToken, fileName);
        AssertNoLazyReuseBranch(generated);
    }

    [TestMethod]
    public void VolatileFilterAndWindowSamples_ShouldKeepVolatileReadsVisible()
    {
        StringAssert.Contains(ReadSample("Q253_VolatileFilterProjectionReuse.cs"), "a.VolatileValue");
        StringAssert.Contains(ReadSample("Q255_VolatileWindowInputs.cs"), "a.VolatileValue");
        AssertNoLazyReuseBranch(ReadSample("Q253_VolatileFilterProjectionReuse.cs"));
        AssertNoLazyReuseBranch(ReadSample("Q255_VolatileWindowInputs.cs"));
    }

    [TestMethod]
    public void GuardedApplySamples_ShouldUseOrdinaryLocalsWithoutRuntimeReuseState()
    {
        var stable = ReadSample("Q258_GuardedStableApplyPredicate.cs");
        var volatileOuter = ReadSample("Q259_GuardedVolatileOuterApplyPredicate.cs");

        StringAssert.Contains(stable, "aValue");
        StringAssert.Contains(volatileOuter, "a_VolatileValue");
        AssertNoLazyReuseBranch(stable);
        AssertNoLazyReuseBranch(volatileOuter);
    }

    private static string ReadSample(string fileName)
    {
        return File.ReadAllText(Path.Combine(GeneratedCodeSampleArtifacts.SamplesDirectory, fileName));
    }

    private static void AssertNoLazyReuseBranch(string generated)
    {
        Assert.IsFalse(generated.Contains("??=", StringComparison.Ordinal));
        Assert.IsFalse(generated.Contains("initialized", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(generated.Contains("reuseCache", StringComparison.OrdinalIgnoreCase));
    }
}
