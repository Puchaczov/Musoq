using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Schema;
using BoundaryRowShapePlanner = Musoq.Evaluator.IR.Planning.BoundaryRowShapePlanner;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class RowWidthPruningPlannerTests
{
    [TestMethod]
    public void Plan_WhenHashJoinBuildHasDroppablePayloadColumns_ShouldApplyPruning()
    {
        var result = RowWidthPruningPlanner.Plan(
        [
            new BoundaryRowShapePlan(
                "hash-join-build:0",
                BoundaryRowShapeKind.HashJoinBuild,
                ["b.City", "b.Id"],
                ["a.Name", "b.City"],
                ["b.Id"],
                ["b.Id"],
                PlanningConfidence.Medium,
                "Hash join build boundary has a key-only payload column.")
        ]);

        var plan = result.Plans.Single();

        Assert.AreEqual(RowWidthPruningStrategy.Applied, plan.Strategy);
        Assert.AreEqual(PlanningConfidence.High, plan.Confidence);
        CollectionAssert.AreEqual(new[] { "b.Id" }, plan.CandidateColumns);
        CollectionAssert.AreEqual(new[] { "b.Id" }, plan.PrunedColumns);
        CollectionAssert.AreEqual(new[] { "a.Name", "b.City" }, plan.RetainedColumns);
        Assert.Contains("drops build-side payload columns", plan.Reason);
        Assert.AreEqual("Applied", result.Decisions.Single().Outcome);
    }

    [TestMethod]
    public void Plan_WhenAggregateHasDroppableColumns_ShouldApplyPruning()
    {
        var result = RowWidthPruningPlanner.Plan(
        [
            new BoundaryRowShapePlan(
                "aggregate:0",
                BoundaryRowShapeKind.Aggregate,
                ["e.Age", "e.City"],
                ["e.City"],
                ["e.Age"],
                ["e.Age"],
                PlanningConfidence.Medium,
                "Aggregate boundary has a future opportunity.")
        ]);

        var plan = result.Plans.Single();

        Assert.AreEqual(RowWidthPruningStrategy.Applied, plan.Strategy);
        CollectionAssert.AreEqual(new[] { "e.Age" }, plan.CandidateColumns);
        CollectionAssert.AreEqual(new[] { "e.Age" }, plan.PrunedColumns);
        CollectionAssert.AreEqual(new[] { "e.City" }, plan.RetainedColumns);
        Assert.Contains("aggregate input-only columns", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenDistinctHasPostBoundaryPayload_ShouldApplyPruning()
    {
        var result = RowWidthPruningPlanner.Plan(
        [
            new BoundaryRowShapePlan(
                "distinct:0",
                BoundaryRowShapeKind.Distinct,
                ["e.City", "e.Payload"],
                ["e.City"],
                ["e.Payload"],
                ["e.Payload"],
                PlanningConfidence.Medium,
                "Distinct boundary has a post-boundary payload pruning opportunity.")
        ]);

        var plan = result.Plans.Single();

        Assert.AreEqual(RowWidthPruningStrategy.Applied, plan.Strategy);
        CollectionAssert.AreEqual(new[] { "e.Payload" }, plan.CandidateColumns);
        CollectionAssert.AreEqual(new[] { "e.Payload" }, plan.PrunedColumns);
        CollectionAssert.AreEqual(new[] { "e.City" }, plan.RetainedColumns);
        Assert.Contains("post-distinct columns", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenSetOperationHasSymmetricPayload_ShouldApplyPruning()
    {
        var result = RowWidthPruningPlanner.Plan(
        [
            new BoundaryRowShapePlan(
                "setoperation:0",
                BoundaryRowShapeKind.SetOperation,
                ["left.City", "left.Payload", "right.City", "right.Payload"],
                ["left.City", "right.City"],
                ["left.City", "right.City"],
                ["left.Payload", "right.Payload"],
                PlanningConfidence.Medium,
                "Set operation boundary has a future symmetric pruning opportunity.")
        ]);

        var plan = result.Plans.Single();

        Assert.AreEqual(RowWidthPruningStrategy.Applied, plan.Strategy);
        CollectionAssert.AreEqual(new[] { "left.Payload", "right.Payload" }, plan.CandidateColumns);
        CollectionAssert.AreEqual(new[] { "left.Payload", "right.Payload" }, plan.PrunedColumns);
        CollectionAssert.AreEqual(new[] { "left.City", "right.City" }, plan.RetainedColumns);
        Assert.Contains("symmetric arm columns", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenWindowHasDroppableColumns_ShouldApplyPruning()
    {
        var result = RowWidthPruningPlanner.Plan(
        [
            new BoundaryRowShapePlan(
                "window:0",
                BoundaryRowShapeKind.Window,
                ["e.City", "e.Population", "e.Country"],
                ["e.City"],
                ["e.Population"],
                ["e.Population", "e.Country"],
                PlanningConfidence.Medium,
                "Window boundary has a future opportunity.")
        ]);

        var plan = result.Plans.Single();

        Assert.AreEqual(RowWidthPruningStrategy.Applied, plan.Strategy);
        CollectionAssert.AreEqual(new[] { "e.Population", "e.Country" }, plan.CandidateColumns);
        CollectionAssert.AreEqual(new[] { "e.Population", "e.Country" }, plan.PrunedColumns);
        CollectionAssert.AreEqual(new[] { "e.City" }, plan.RetainedColumns);
        Assert.Contains("drops window-only columns", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenCteMaterializationHasDroppableColumns_ShouldApplyPruning()
    {
        var result = RowWidthPruningPlanner.Plan(
        [
            new BoundaryRowShapePlan(
                "cte:orders",
                BoundaryRowShapeKind.CteMaterialization,
                ["cte.Id", "cte.City", "cte.Payload"],
                ["cte.Id"],
                [],
                ["cte.City", "cte.Payload"],
                PlanningConfidence.Medium,
                "CTE materialization has a future opportunity.")
        ]);

        var plan = result.Plans.Single();

        Assert.AreEqual(RowWidthPruningStrategy.Applied, plan.Strategy);
        CollectionAssert.AreEqual(new[] { "cte.City", "cte.Payload" }, plan.CandidateColumns);
        CollectionAssert.AreEqual(new[] { "cte.City", "cte.Payload" }, plan.PrunedColumns);
        CollectionAssert.AreEqual(new[] { "cte.Id" }, plan.RetainedColumns);
        Assert.Contains("drops materialized columns", plan.Reason);
    }

    [TestMethod]
    public void BoundaryPlan_WhenHashJoinResidualUsesBuildPayload_ShouldRetainResidualColumn()
    {
        var left = CreateValuesScan("a", ("Id", typeof(int)), ("Name", typeof(string)));
        var right = CreateValuesScan("b", ("Id", typeof(int)), ("Payload", typeof(string)));
        var join = new PhysicalHashJoinNode(
            JoinKind.Inner,
            [new ColumnRef("b", "Id", typeof(int))],
            [new ColumnRef("a", "Id", typeof(int))],
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("b", "Payload", typeof(string)),
                new Literal("keep", typeof(string)),
                typeof(bool)),
            left,
            right);
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("a", "Name", typeof(string)), 0)],
            join);

        var rowShape = BoundaryRowShapePlanner.Plan(project, CreateEmptyProperties().RequiredColumns);
        var pruning = RowWidthPruningPlanner.Plan(rowShape.Plans);
        var plan = pruning.Plans.Single(static item => item.Kind == BoundaryRowShapeKind.HashJoinBuild);

        Assert.IsTrue(plan.RetainedColumns.Contains("b.Payload"));
        Assert.IsTrue(plan.PrunedColumns.Contains("b.Id"));
        Assert.IsFalse(plan.PrunedColumns.Contains("b.Payload"));
        Assert.Contains(
            "residual predicate columns",
            rowShape.Plans.Single(static item => item.Kind == BoundaryRowShapeKind.HashJoinBuild).Reason);
    }

    [TestMethod]
    public void BoundaryPlan_WhenHashJoinProbeHasUnusedPayload_ShouldApplyProbePayloadPruning()
    {
        var left = CreateValuesScan(
            "a",
            ("Id", typeof(int)),
            ("Name", typeof(string)),
            ("Payload", typeof(string)));
        var right = CreateValuesScan("b", ("Id", typeof(int)));
        var join = new PhysicalHashJoinNode(
            JoinKind.Inner,
            [new ColumnRef("b", "Id", typeof(int))],
            [new ColumnRef("a", "Id", typeof(int))],
            null,
            left,
            right);
        var project = new PhysicalProjectNode(
            [new ProjectedField("Id", new ColumnRef("b", "Id", typeof(int)), 0)],
            join);

        var rowShape = BoundaryRowShapePlanner.Plan(project, CreateEmptyProperties().RequiredColumns);
        var probePlan = rowShape.Plans.Single(static item => item.Kind == BoundaryRowShapeKind.HashJoinProbe);
        var pruning = RowWidthPruningPlanner.Plan(rowShape.Plans);
        var pruningPlan = pruning.Plans.Single(static item => item.Kind == BoundaryRowShapeKind.HashJoinProbe);

        Assert.Contains("probe-key columns", probePlan.Reason);
        CollectionAssert.AreEqual(new[] { "a.Id", "a.Name", "a.Payload" }, probePlan.FutureDroppableColumns);
        Assert.IsEmpty(probePlan.BlockedColumns);
        Assert.AreEqual(RowWidthPruningStrategy.Applied, pruningPlan.Strategy);
        CollectionAssert.AreEqual(new[] { "a.Id", "a.Name", "a.Payload" }, pruningPlan.PrunedColumns);
        Assert.Contains("probe-side payload columns", pruningPlan.Reason);
    }

    private static PhysicalValuesScanNode CreateValuesScan(
        string alias,
        params (string Name, Type Type)[] columns)
    {
        var schemaColumns = new ColumnSchema[columns.Length];

        for (var i = 0; i < columns.Length; i++)
            schemaColumns[i] = new ColumnSchema(columns[i].Name, columns[i].Type, i);

        return new PhysicalValuesScanNode(alias, [], new OutputSchema(schemaColumns));
    }

    private static PlanProperties CreateEmptyProperties()
    {
        return PlanPropertiesTestFactory.CreateEmpty();
    }
}
