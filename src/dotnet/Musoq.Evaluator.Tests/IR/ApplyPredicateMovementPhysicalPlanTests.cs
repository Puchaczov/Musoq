using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ApplyPredicateMovementPhysicalPlanTests
{
    [TestMethod]
    public void Lower_WhenNestedApplyPlansAreProvided_ShouldAttachEachPlanToItsBoundary()
    {
        var left = CreateLogicalScan("a");
        var middle = CreateLogicalScan("b");
        var right = CreateLogicalScan("c");
        var firstApply = new ApplyNode(ApplyKind.Cross, left, middle);
        var secondApply = new ApplyNode(ApplyKind.Cross, firstApply, right);
        var firstPlan = CreatePlan("apply-1", firstApply, "a.Name = 'keep'", "a");
        var secondPlan = CreatePlan("apply-2", secondApply, "b.X = 1", "b");

        var physical = (PhysicalNestedLoopApplyNode)new PhysicalPlanBuilder(
            predicateMovementPlans: null,
            strategyPlan: new PhysicalStrategyPlan(),
            applyPredicateMovementPlans: [secondPlan, firstPlan])
            .Lower(secondApply);

        var physicalFirstApply = (PhysicalNestedLoopApplyNode)physical.Left;
        Assert.AreSame(firstPlan, physicalFirstApply.ApplyPredicateMovementPlans[0]);
        Assert.AreSame(secondPlan, physical.ApplyPredicateMovementPlans[0]);
    }

    [TestMethod]
    public void Lower_WhenTheSamePredicateIsPlannedTwiceForOneApply_ShouldKeepOnePhysicalGuard()
    {
        var left = CreateLogicalScan("a");
        var right = CreateLogicalScan("b");
        var apply = new ApplyNode(ApplyKind.Cross, left, right);
        var first = CreatePlan("apply-1", apply, "a.Name = 'keep'", "a");
        var duplicate = CreatePlan("apply-2", apply, "a.Name = 'keep'", "a");

        var physical = (PhysicalNestedLoopApplyNode)new PhysicalPlanBuilder(
            predicateMovementPlans: null,
            strategyPlan: new PhysicalStrategyPlan(),
            applyPredicateMovementPlans: [duplicate, first])
            .Lower(apply);

        Assert.AreEqual(1, physical.ApplyPredicateMovementPlans.Count);
        Assert.AreSame(first, physical.ApplyPredicateMovementPlans[0]);
    }

    [TestMethod]
    public void RewriteChildren_WhenApplyChildrenChange_ShouldPreserveMovementPlans()
    {
        var left = CreatePhysicalScan("a");
        var right = CreatePhysicalScan("b");
        var logicalApply = new ApplyNode(
            ApplyKind.Cross,
            CreateLogicalScan("a"),
            CreateLogicalScan("b"));
        var plan = CreatePlan("apply-1", logicalApply, "a.Name = 'keep'", "a");
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Cross, left, right)
        {
            ApplyPredicateMovementPlans = [plan]
        };

        var rewritten = PhysicalPlanRewriter.RewriteChildren(
            apply,
            node => ReferenceEquals(node, left) ? left with { Alias = "rewritten" } : node);

        var rewrittenApply = (PhysicalNestedLoopApplyNode)rewritten;
        Assert.AreSame(plan, rewrittenApply.ApplyPredicateMovementPlans[0]);
        Assert.AreEqual("rewritten", ((PhysicalSchemaScanNode)rewrittenApply.Left).Alias);
    }

    [TestMethod]
    public void ApplyChainSourceCollector_ShouldKeepGuardsWithTheirFlattenedRightBoundary()
    {
        var firstApply = new ApplyNode(
            ApplyKind.Cross,
            CreateLogicalScan("a"),
            CreateLogicalScan("b"));
        var secondApply = new ApplyNode(
            ApplyKind.Cross,
            firstApply,
            CreateLogicalScan("c"));
        var firstPlan = CreatePlan("apply-1", firstApply, "a.Name = 'keep'", "a");
        var secondPlan = CreatePlan("apply-2", secondApply, "b.X = 1", "b");
        var left = CreatePhysicalScan("a");
        var middle = CreatePhysicalScan("b");
        var right = CreatePhysicalScan("c");
        var nested = new PhysicalNestedLoopApplyNode(ApplyKind.Cross, left, middle)
        {
            ApplyPredicateMovementPlans = [firstPlan]
        };
        var root = new PhysicalNestedLoopApplyNode(ApplyKind.Cross, nested, right)
        {
            ApplyPredicateMovementPlans = [secondPlan]
        };

        var collector = new ApplyChainSourceCollector();
        Assert.IsTrue(collector.TryCollectCrossApplySources(root, out var sources));
        Assert.IsEmpty(sources[0].ApplyPredicateMovementPlans);
        Assert.AreSame(firstPlan, sources[1].ApplyPredicateMovementPlans[0]);
        Assert.AreSame(secondPlan, sources[2].ApplyPredicateMovementPlans[0]);
    }

    [TestMethod]
    public void Print_WhenApplyHasMovementPlans_ShouldIncludeGuardPlacementAndPredicate()
    {
        var apply = new PhysicalNestedLoopApplyNode(
            ApplyKind.Cross,
            CreatePhysicalScan("a"),
            CreatePhysicalScan("b"))
        {
            ApplyPredicateMovementPlans =
            [
                CreatePlan(
                    "apply-1",
                    new ApplyNode(ApplyKind.Cross, CreateLogicalScan("a"), CreateLogicalScan("b")),
                    "a.Name = 'keep'",
                    "a")
            ]
        };

        var text = PhysicalPlanPrinter.Print(apply);

        StringAssert.Contains(text, "PhysicalNestedLoopApply [Cross] [guards: PreApplyRight: a.Name = 'keep']");
    }

    private static ApplyPredicateMovementPlan CreatePlan(
        string movementId,
        ApplyNode apply,
        string predicateText,
        string alias)
    {
        return new ApplyPredicateMovementPlan(
            movementId,
            apply,
            PredicatePlacementOrigin.Where,
            PredicateEarliestPlacement.PreApplyRight,
            [alias],
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef(alias, "Name", typeof(string)),
                new Literal("keep", typeof(string)),
                typeof(bool)),
            predicateText,
            PlanningConfidence.High,
            "test movement plan");
    }

    private static SchemaScanNode CreateLogicalScan(string alias)
    {
        return new SchemaScanNode(
            "test",
            "items",
            [],
            alias,
            new OutputSchema([new ColumnSchema("Name", typeof(string), 0)]));
    }

    private static PhysicalSchemaScanNode CreatePhysicalScan(string alias)
    {
        return new PhysicalSchemaScanNode(
            "test",
            "items",
            [],
            alias,
            [],
            [],
            new OutputSchema([new ColumnSchema("Name", typeof(string), 0)]));
    }
}
