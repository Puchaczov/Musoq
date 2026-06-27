using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void CteSidecarStagedGraphMixedSample_WhenSidecarParallelizationIsEnabled_ShouldRespectDependencyLevels()
    {
        var result = CompileSampleForInspection(
            CteSidecarStagedGraphMixedSampleFileName,
            CreateParallelCteSidecarOptions());

        Assert.Contains("ParallelEligibility [ParallelCte] PhysicalCteNode -> Candidate", result.PlanningText);
        Assert.Contains("private static List<Cte0Row0> BuildCte0(", result.GeneratedCSharpCode);
        Assert.Contains("ParallelBlock [cte-level-1, tasks 3, maxDegree 3]", result.ExecutionPlanText);
        Assert.Contains("ParallelTask [names -> __parallelCteLevel1Task0Result]", result.ExecutionPlanText);
        Assert.Contains("ParallelTask [cities -> __parallelCteLevel1Task1Result]", result.ExecutionPlanText);
        Assert.Contains("ParallelTask [eligible -> __parallelCteLevel1Task2Result]", result.ExecutionPlanText);
        Assert.Contains("private static object BuildCteLevel1Task0(", result.GeneratedCSharpCode);
        Assert.Contains("private static object BuildCteLevel1Task1(", result.GeneratedCSharpCode);
        Assert.Contains("private static object BuildCteLevel1Task2(", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot1 = __parallelCteLevel1Task0Result", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot2 = __parallelCteLevel1Task1Result", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot3 = __parallelCteLevel1Task2Result", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Cte3Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        AssertGeneratedCodeUsesTypedCteIndexResults(result.GeneratedCSharpCode);
        AssertGeneratedCodeUsesTypedCteRowResults(result.GeneratedCSharpCode);
    }
}
