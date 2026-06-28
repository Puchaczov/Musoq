using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenComputedUnionAllStreamsDirectly_ShouldNotWarn()
    {
        var result = Inspect(CreateComputedUnionAllQuery(), new CompilationOptions());

        Assert.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> StreamingUnionAll", result.PlanningText);
        Assert.Contains("UnionAll arms use directly streamable row sources", result.PlanningText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("SetOperation [result = left UnionAll right", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("UnionAll(left, right", StringComparison.Ordinal));
        AssertNoFallbackWarning(result);
    }

    [TestMethod]
    public void CompileForInspection_WhenUnionAllStreamsDirectly_ShouldNotWarn()
    {
        var result = Inspect(
            "select d.Dummy as Dummy from #system.dual() d union all (Dummy) select e.Dummy as Dummy from #system.dual() e",
            new CompilationOptions());

        Assert.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> StreamingUnionAll", result.PlanningText);
        AssertNoFallbackWarning(result);
    }

    [TestMethod]
    public void CompileForInspection_WhenSetOperationUsesHashSet_ShouldNotWarn()
    {
        var result = Inspect(
            "select d.Dummy as Dummy from #system.dual() d union (Dummy) select e.Dummy as Dummy from #system.dual() e",
            new CompilationOptions());

        Assert.Contains("SetOperationStrategy [SetOperationStrategy] Union -> HashSet", result.PlanningText);
        AssertNoFallbackWarning(result);
    }

    [TestMethod]
    public void CompileForInspection_WhenSingleUseCteCannotFuseReadOnceProjection_ShouldNotWarn()
    {
        var result = Inspect(
            "with p as (select d.Dummy as Dummy from #system.dual() d) select p.Dummy from p order by p.Dummy");

        Assert.Contains("CteStrategy [CteReuseStrategy] cte:p -> MaterializeSingleUse", result.PlanningText);
        Assert.Contains("Single-use CTE materializes because it is not the terminal read-once projection candidate.", result.PlanningText);
        AssertNoFallbackWarning(result);
    }

    [TestMethod]
    public void CompileForInspection_WhenSingleUseCteFusesReadOnceProjection_ShouldNotWarn()
    {
        var result = Inspect(
            "with p as (select d.Dummy as Dummy from #system.dual() d) select p.Dummy from p");

        Assert.Contains("CteStrategy [CteReuseStrategy] cte:p -> FuseReadOnce", result.PlanningText);
        AssertNoFallbackWarning(result);
    }

    [TestMethod]
    public void CompileForInspection_WhenReadOnceCteProjectsNonDeterministicValueTwice_ShouldMaterialize()
    {
        var result = Inspect(
            "with p as (select Rand() as r from #system.dual() d) select p.r, p.r as again from p");

        AssertNonDeterministicReadOnceCteMaterializes(result);
    }

    [TestMethod]
    public void CompileForInspection_WhenReadOnceCteFiltersNonDeterministicAliasAgainstItself_ShouldMaterialize()
    {
        var result = Inspect(
            "with p as (select Rand() as r from #system.dual() d) select p.r from p where p.r = p.r");

        AssertNonDeterministicReadOnceCteMaterializes(result);
    }

    [TestMethod]
    public void CompileForInspection_WhenReadOnceCtePrunesNonDeterministicFilteredColumn_ShouldMaterialize()
    {
        var result = Inspect(
            "with p as (select Rand() as r, d.Dummy as Dummy from #system.dual() d) select p.Dummy from p where p.r = p.r");

        AssertNonDeterministicReadOnceCteMaterializes(result);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteIsReused_ShouldNotWarnForCteReuseMaterialization()
    {
        var result = Inspect(
            "with p as (select d.Dummy as Dummy from #system.dual() d) select a.Dummy, b.Dummy from p a inner join p b on a.Dummy = b.Dummy");

        Assert.Contains("CteStrategy [CteReuseStrategy] cte:p -> MaterializeReuse", result.PlanningText);
        AssertNoFallbackWarning(result);
    }

    private static void AssertNonDeterministicReadOnceCteMaterializes(QueryInspectionResult result)
    {
        Assert.Contains("CteStrategy [CteReuseStrategy] cte:p -> MaterializeSingleUse", result.PlanningText);
        Assert.Contains("SideEffectSensitive", result.PlanningText);
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.IsFalse(result.PlanningText.Contains("cte:p -> FuseReadOnce", StringComparison.Ordinal), result.PlanningText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CteReadOnceFusionCandidate", StringComparison.Ordinal), result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenPredicateSubqueryUsesLeftApply_ShouldNotWarn()
    {
        var result = Inspect(
            """
            select d.Dummy
            from #system.dual() d
            where exists (
                select e.Dummy from #system.dual() e
                where e.Dummy = d.Dummy
            ) or d.Dummy = 'missing'
            """);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> PredicateLeftApply", result.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftOuter]", result.PhysicalPlanText);
        AssertNoFallbackWarning(result);
    }
}
