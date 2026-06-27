using System;
using System.Collections.Generic;
using System.Linq;
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

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> PredicateAntiSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenCorrelatedInSubqueryHasResidualPredicate_ShouldKeepHashSemiJoinStrategy()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (
                SELECT b.City FROM #B.entities() b
                WHERE b.Population < a.Population
            )";

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> PredicateSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("[residual:", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenScalarAggregateIsCorrelated_ShouldExposeScalarLeftJoinStrategy()
    {
        const string query = @"
            SELECT a.City, (
                SELECT Sum(b.Population) FROM #B.entities() b
                WHERE b.Country = a.Country
            ) AS TotalPopulation
            FROM #A.entities() a";

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> ScalarLeftJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftOuter]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_value", inspection.PhysicalPlanText);
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

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _dt_1 -> DerivedTableJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [Inner]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenDerivedApplyFeedsHashBuild_ShouldExposeSingleUseHashBuildFusionCandidate()
    {
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

    [TestMethod]
    public void WhenCompositeExistsSubqueryUsesReferenceAndValueKeys_ShouldUseTypedHashKeyAndPreserveNullSemantics()
    {
        const string query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE EXISTS (
                SELECT b.Name FROM #B.entities() b
                WHERE b.Country = a.Country
                  AND b.Population = a.Population
            )";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "match", Country = "PL", Population = 10m },
                    new BasicEntity { Name = "left-null", Country = null, Population = 20m },
                    new BasicEntity { Name = "no-match", Country = "DE", Population = 30m }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity { Name = "right-match", Country = "PL", Population = 10m },
                    new BasicEntity { Name = "right-null", Country = null, Population = 20m }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);
        var inspection = CompileSubqueryForInspection(query);

        CollectionAssert.AreEqual(new[] { "match" }, table.Select(row => (string)row.Values[0]).ToArray());
        Assert.Contains("ValueTuple<int, string, decimal>", inspection.ExecutionPlanText);
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("CreateNullableHashJoinKey", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("Dictionary<object, HashJoinBucket<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenCompositeNotExistsSubqueryUsesReferenceAndValueKeys_ShouldTreatNullProbeAsNoMatch()
    {
        const string query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE NOT EXISTS (
                SELECT b.Name FROM #B.entities() b
                WHERE b.Country = a.Country
                  AND b.Population = a.Population
            )";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "match", Country = "PL", Population = 10m },
                    new BasicEntity { Name = "left-null", Country = null, Population = 20m },
                    new BasicEntity { Name = "no-match", Country = "DE", Population = 30m }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity { Name = "right-match", Country = "PL", Population = 10m },
                    new BasicEntity { Name = "right-null", Country = null, Population = 20m }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);
        var inspection = CompileSubqueryForInspection(query);

        CollectionAssert.AreEqual(
            new[] { "left-null", "no-match" },
            table.Select(row => (string)row.Values[0]).ToArray());
        Assert.Contains("ValueTuple<int, string, decimal>", inspection.ExecutionPlanText);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
    }

    private static void AssertNoPerRowSubqueryExecution(QueryInspectionResult inspection)
    {
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("PhysicalNestedLoopApply", StringComparison.Ordinal));
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("PhysicalNestedLoopJoin", StringComparison.Ordinal));
        Assert.IsFalse(inspection.ExecutionPlanText.Contains("NestedLoop", StringComparison.Ordinal));
    }
}
