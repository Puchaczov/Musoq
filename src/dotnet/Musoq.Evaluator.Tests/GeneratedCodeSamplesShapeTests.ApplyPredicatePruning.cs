using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void CrossApplyWhereLeftGuardSample_WhenCompiled_ShouldGuardBeforePropertySource()
    {
        var result = CompileSampleForInspection(CrossApplyWhereLeftGuardSampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        AssertGuardBeforeSource(result.ExecutionPlanText, "i.Name = 'left'", "i.Numbers");
        Assert.Contains("continue;", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void ChainedCrossApplyScopedGuardsSample_WhenCompiled_ShouldEmitTwoEarlyGuards()
    {
        var result = CompileSampleForInspection(ChainedCrossApplyScopedGuardsSampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        Assert.AreEqual(2, CountOccurrences(result.ExecutionPlanText, "ContinueIf [NOT"));
        Assert.IsGreaterThanOrEqualTo(2, CountOccurrences(result.GeneratedCSharpCode, "continue;"));
        Assert.Contains("i.Name = 'left'", result.PlanningText);
        Assert.Contains("n.Value = 1", result.PlanningText);
    }

    [TestMethod]
    public void ChainedCrossApplyResidualPredicateSample_WhenCompiled_ShouldKeepFinalPredicateResidual()
    {
        var result = CompileSampleForInspection(ChainedCrossApplyResidualPredicateSampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        Assert.AreEqual(2, CountOccurrences(result.ExecutionPlanText, "ContinueIf [NOT"));
        Assert.Contains("If [(value = 2)]", result.ExecutionPlanText);
        Assert.Contains("value == 2", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void OuterApplyWhereLeftGuardSample_WhenCompiled_ShouldGuardBeforeEmptyRightSource()
    {
        var result = CompileSampleForInspection(OuterApplyWhereLeftGuardSampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        AssertGuardBeforeSource(result.ExecutionPlanText, "i.Name = 'empty'", "i.Numbers");
        Assert.Contains("continue;", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CrossApplyMethodWhereLeftGuardSample_WhenCompiled_ShouldGuardBeforeAccessMethodSource()
    {
        var result = CompileSampleForInspection(CrossApplyMethodWhereLeftGuardSampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        AssertGuardBeforeSource(result.ExecutionPlanText, "i.Name = 'left'", "JustReturnArrayOfString()");
        Assert.Contains("continue;", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CrossApplyPredicatePruningSamples_WhenCheckedIn_ShouldExposeContinueGuards()
    {
        foreach (var fileName in new[]
                 {
                     CrossApplyWhereLeftGuardSampleFileName,
                     ChainedCrossApplyScopedGuardsSampleFileName,
                     ChainedCrossApplyResidualPredicateSampleFileName,
                     OuterApplyWhereLeftGuardSampleFileName,
                     CrossApplyMethodWhereLeftGuardSampleFileName
                 })
        {
            var sample = ReadSample(fileName).Content;
            Assert.Contains("continue;", sample, fileName);
        }
    }

    private static void AssertGuardBeforeSource(string planText, string predicate, string source)
    {
        var guardIndex = planText.IndexOf($"ContinueIf [NOT ({predicate})]", StringComparison.Ordinal);
        var sourceIndex = planText.IndexOf($"EnumerableSource [{source}", StringComparison.Ordinal);

        Assert.IsTrue(guardIndex >= 0 && guardIndex < sourceIndex, planText);
    }
}
