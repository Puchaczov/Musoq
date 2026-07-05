using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenCteSidecarIndexEnabledButHashBuildKeyIsNotSimpleColumn_ShouldNotWarn()
    {
        var result = Inspect(CreateCteSidecarIneligibleBuildKeyQuery(), CreateCteSidecarOptions());

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("The hash-build key is not a simple CTE output column reference.", result.PlanningText);
        AssertNoFallbackWarning(result, "CteSidecarIndexStrategy");
        AssertExecutionPlanDoesNotContain("StoreCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("LoadCteIndex [", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteSidecarIndexesAreDisabledForSameShape_ShouldNotWarn()
    {
        var result = Inspect(CreateCteSidecarIneligibleBuildKeyQuery(), CreateCteSidecarDisabledOptions());

        Assert.IsFalse(result.PlanningText.Contains("CteSidecarIndexStrategy", System.StringComparison.Ordinal));
        AssertNoFallbackWarning(result, "CteSidecarIndexStrategy");
    }

    [TestMethod]
    public void CompileForInspection_WhenCteSidecarIndexPathIsEligible_ShouldNotWarn()
    {
        var result = Inspect(CreateCteBackedInnerHashJoinQuery(), CreateCteSidecarOptions());

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("-> Hash", result.PlanningText);
        AssertNoFallbackWarning(result, "CteSidecarIndexStrategy");
    }

    private static string CreateCteSidecarIneligibleBuildKeyQuery()
    {
        return """
            with leftCte as (
                select d.Dummy as Dummy
                from #system.dual() d
            ),
            rightCte as (
                select e.Dummy as Dummy
                from #system.dual() e
            )
            select l.Dummy, r.Dummy
            from leftCte l
            inner join rightCte r on l.Dummy = r.Dummy + ''
            """;
    }
}
