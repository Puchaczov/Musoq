using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenInSubqueryIsDecorrelatable_ShouldExposeSemiJoinStrategyAndAvoidPerRowApply()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> PredicateSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenNotExistsSubqueryIsDecorrelatable_ShouldExposeAntiSemiJoinStrategy()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE NOT EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            )";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["BERLIN"]);

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> PredicateAntiSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenCorrelatedInSubqueryHasResidualPredicate_ShouldUsePartitionedRangeSemiJoinStrategy()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (
                SELECT b.City FROM #B.entities() b
                WHERE b.Population < a.Population
            )";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> PredicateRangeSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalSortMergeJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("[residual:", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenScalarAggregateIsCorrelated_ShouldExposeScalarHashSingleStrategy()
    {
        const string query = @"
            SELECT a.City, (
                SELECT Sum(b.Population) FROM #B.entities() b
                WHERE b.Country = a.Country
            ) AS TotalPopulation
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", 210m],
            new object?[] { "BERLIN", null },
            ["PARIS", 450m]);

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> ScalarHashSingle", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_value", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenOrderedScalarIsCorrelated_ShouldRankPartitionsOnceAndHashProbeResults()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                ORDER BY b.Population DESC
                TAKE 1
            ) AS LargestCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("LargestCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "GDANSK"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("PhysicalWindow", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        Assert.Contains("WindowMaterialization", inspection.PlanningText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenGroupedScalarIsCorrelated_ShouldAggregateSetWiseAndHashProbeResults()
    {
        const string query = @"
            SELECT a.City, (
                SELECT Max(b.Population) FROM #B.entities() b
                WHERE b.Country = a.Country
            GROUP BY b.Country
            ) AS LargestPopulation
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("LargestPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", 110m],
            new object?[] { "BERLIN", null },
            ["PARIS", 450m]);

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("PhysicalSingleKeyAggregate", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenScalarSetOperatorIsCorrelated_ShouldCombineBranchesOnceAndHashProbeResults()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                INTERSECT (City)
                SELECT c.City FROM #C.entities() c
                WHERE c.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("PhysicalSetOp [Intersect]", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenApplyDerivedTableIsCorrelated_ShouldExposeDerivedJoinStrategy()
    {
        const string query = @"
            SELECT a.City, d.City FROM #A.entities() a
            CROSS APPLY (
                SELECT b.City, b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
            ) d";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("d.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            ["WARSAW", "GDANSK"],
            ["PARIS", "PARIS"]);

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _dt_1 -> DerivedTableJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [Inner]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenDerivedApplyFeedsHashBuild_ShouldExposeSingleUseHashBuildFusionCandidate()
    {
        // Plan-only by design: execution semantics are covered by
        // WhenApplyDerivedTableIsCorrelated; this test isolates the fusion candidate boundary.
        const string query = @"
            SELECT a.City, d.City FROM #A.entities() a
            CROSS APPLY (
                SELECT b.City, b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
            ) d";

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains(
            "Materialization [SingleUseHashBuildFusion] cte:_dt_1 -> Candidate",
            inspection.PlanningText);
        Assert.Contains("SingleUseFusionCandidate [cte0]", inspection.InitialExecutionPlanText);
        Assert.Contains("CtePhase [cte0]", inspection.OptimizedExecutionPlanText);
        Assert.IsFalse(
            inspection.OptimizedExecutionPlanText.Contains("SingleUseFusionCandidate", StringComparison.Ordinal),
            inspection.OptimizedExecutionPlanText);
    }

    [TestMethod]
    public void WhenScalarSubqueryJoinOnCreatesSingleUseStages_ShouldExposeFusionCandidates()
    {
        // Plan-only by design: the companion scalar result-shaping tests cover execution;
        // this test verifies the optimizer's single-use staging decisions.
        const string query = @"
            SELECT a.City, b.City
            FROM #A.entities() a
            INNER JOIN #B.entities() b ON b.City = (
                SELECT c.City
                FROM #C.entities() c
                WHERE c.Country = a.Country
            )";

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains(
            "Materialization [SingleUseHashBuildFusion] cte:_sq_1 -> Candidate",
            inspection.PlanningText);
        Assert.Contains(
            "Materialization [SingleUseHashBuildFusion] statement:a_sq_1 -> Candidate",
            inspection.PlanningText);
        Assert.Contains(
            "Materialization [SingleUseProjectionFusion] statement:a_sq_1b -> Candidate",
            inspection.PlanningText);
        Assert.Contains("SingleUseFusionCandidate [cte2]", inspection.InitialExecutionPlanText);
        Assert.Contains("CtePhase [cte2]", inspection.OptimizedExecutionPlanText);
        Assert.IsFalse(
            inspection.OptimizedExecutionPlanText.Contains("SingleUseFusionCandidate", StringComparison.Ordinal),
            inspection.OptimizedExecutionPlanText);
    }

    [TestMethod]
    public void WhenCteIsReused_ShouldNotExposeSingleUseFusionCandidate()
    {
        // Plan-only by design: CTE reuse is an optimizer boundary with no distinct result contract.
        const string query = @"
            WITH people AS (
                SELECT p.Name, p.Country FROM #A.entities() p
            )
            SELECT a.Name, b.Name
            FROM people a
            INNER JOIN people b ON a.Country = b.Country";

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("Materialization [CteReuseBoundary] cte:people -> Required", inspection.PlanningText);
        Assert.IsFalse(
            inspection.PlanningText.Contains("SingleUseHashBuildFusion] cte:people -> Candidate", StringComparison.Ordinal),
            inspection.PlanningText);
        Assert.IsFalse(
            inspection.PlanningText.Contains("SingleUseProjectionFusion] cte:people -> Candidate", StringComparison.Ordinal),
            inspection.PlanningText);
    }

    private static void AssertNoPerRowSubqueryExecution(QueryInspectionResult inspection)
    {
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("PhysicalNestedLoopApply", StringComparison.Ordinal));
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("PhysicalNestedLoopJoin", StringComparison.Ordinal));
        Assert.IsFalse(inspection.ExecutionPlanText.Contains("NestedLoop", StringComparison.Ordinal));
    }
}
