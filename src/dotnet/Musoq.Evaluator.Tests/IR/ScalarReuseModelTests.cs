using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using Musoq.Plugins.Attributes;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ScalarReuseModelTests
{
    [TestMethod]
    public void StableCandidateCanMoveFromOwningRegionToDescendant()
    {
        var owner = ScalarEvaluationRegion.Root("outer");
        var descendant = new ScalarEvaluationRegion(
            "inner",
            ScalarEvaluationRegionKind.Unconditional,
            true,
            true,
            owner);
        var candidate = new ScalarReuseCandidate(
            "field:a.Value",
            typeof(int),
            ColumnStability.Stable,
            ["a"],
            "outer",
            owner,
            1,
            3,
            4);

        Assert.IsTrue(candidate.CanMoveTo(descendant));
        Assert.IsTrue(candidate.IsRepeated);
    }

    [TestMethod]
    public void VolatileAndVariableOnlyCandidatesAreRejectedByCostModel()
    {
        var region = ScalarEvaluationRegion.Root("row");
        var model = new ScalarReuseCostModel();
        var volatileCandidate = new ScalarReuseCandidate(
            "random",
            typeof(int),
            ColumnStability.Volatile,
            [],
            "row",
            region,
            4,
            4,
            4);
        var variableCandidate = volatileCandidate with
        {
            Fingerprint = "variable",
            Stability = ColumnStability.Stable,
            IsVariableOnly = true
        };

        Assert.IsFalse(model.ShouldMaterialize(volatileCandidate, 128, 4));
        Assert.IsFalse(model.ShouldMaterialize(variableCandidate, 128, 4));
    }

    [TestMethod]
    public void PhysicalComputePreservesInputColumnsAndAddsComputedFields()
    {
        var input = new PhysicalSchemaScanNode(
            "#test",
            "items",
            [],
            "item",
            [],
            [],
            new OutputSchema([new ColumnSchema("Id", typeof(int), 0)]));
        var compute = new PhysicalComputeNode(
            input,
            [new ProjectedField("Computed", new Literal(1, typeof(int)), 1)]);

        Assert.AreEqual(2, compute.OutputSchema.Columns.Length);
        Assert.AreEqual("Id", compute.OutputSchema.Columns[0].Name);
        Assert.AreEqual("Computed", compute.OutputSchema.Columns[1].Name);
        StringAssert.Contains(PhysicalPlanPrinter.Print(compute), "PhysicalCompute");
    }

    [TestMethod]
    public void PhysicalComputeRewriterIsIdempotentForUnchangedInput()
    {
        var input = new PhysicalSchemaScanNode(
            "#test",
            "items",
            [],
            "item",
            [],
            [],
            new OutputSchema([new ColumnSchema("Id", typeof(int), 0)]));
        var compute = new PhysicalComputeNode(
            input,
            [new ProjectedField("Computed", new Literal(1, typeof(int)), 1)]);

        var rewritten = PhysicalPlanRewriter.RewriteChildren(compute, static child => child);

        Assert.AreSame(compute, rewritten);
    }

    [TestMethod]
    public void BuiltInOptimizationPipelinesHaveEvaluationClassifications()
    {
        foreach (var pass in PhysicalOptimizationGroup.Passes)
            OptimizerClassificationRegistry.Require(pass);

        foreach (var pass in ExecutionIrOptimizationGroup.Passes)
            OptimizerClassificationRegistry.Require(pass);
    }

    [TestMethod]
    public void BoundaryRejectsVolatileAndConditionalCandidates()
    {
        var region = ScalarEvaluationRegion.Root("outer");
        var stable = new ScalarReuseCandidate(
            "stable", typeof(int), ColumnStability.Stable, ["field:a.Value"], "outer", region, 3, 2, 4);
        var volatileCandidate = stable with { Fingerprint = "volatile", Stability = ColumnStability.Volatile };
        var boundary = new ScalarReuseBoundary(
            ScalarReuseBoundaryKind.Projection, "outer", true, true, false);

        Assert.IsTrue(boundary.CanCarry(stable));
        Assert.IsFalse(boundary.CanCarry(volatileCandidate));
        Assert.IsFalse((boundary with { IsConditional = true }).CanCarry(stable));
    }

    [TestMethod]
    public void PivotDispatchSharesOnlyStableDiscriminators()
    {
        var stable = new PivotPredicateDispatch("field:row.Kind", ColumnStability.Stable, ["A", "B"], false);
        var volatileDispatch = stable with { Stability = ColumnStability.Volatile };
        var overlapping = stable with { HasOverlappingPredicates = true };

        Assert.IsTrue(stable.CanShareDiscriminator);
        Assert.IsFalse(volatileDispatch.CanShareDiscriminator);
        Assert.IsTrue(overlapping.RetainsIndependentPredicates);
    }

    [TestMethod]
    public void SpecializedJoinKeysRespectOwningSideAndStability()
    {
        var key = new JoinKeyReuseFact(
            SpecializedJoinKeyKind.AsOfProbe, "probe", ColumnStability.Stable, false, true, "key");
        var innerDependent = key with { DependsOnInnerRow = true };
        var volatileKey = key with { Stability = ColumnStability.Volatile };

        Assert.IsTrue(key.CanReuse);
        Assert.IsFalse(innerDependent.CanReuse);
        Assert.IsFalse(volatileKey.CanReuse);
    }

    [TestMethod]
    public void CorrelationReuseRequiresStableOwnerAndExplicitMaterialization()
    {
        var stable = new CorrelationScalarReuseFact("a.Id", "outer", ColumnStability.Stable, false, true, false);
        var helper = stable with { CrossesHelperBoundary = true };
        var volatileMaterialized = stable with { Stability = ColumnStability.Volatile, IsMaterialized = true };

        Assert.IsTrue(stable.CanCarry);
        Assert.IsFalse(helper.CanCarry);
        Assert.IsTrue(volatileMaterialized.MustEvaluatePerProducedRow);
    }

    [TestMethod]
    public void UnpivotPlanHoistsOnlyStableKeepValues()
    {
        var plan = new UnpivotScalarReusePlan(["keep:id"], ["entry:value"], true);

        Assert.IsTrue(plan.CanHoistKeep("keep:id", ColumnStability.Stable));
        Assert.IsFalse(plan.CanHoistKeep("keep:id", ColumnStability.Volatile));
        Assert.IsTrue(plan.KeepsEntryEvaluationLocal("entry:value"));
    }

    [TestMethod]
    public void BoundaryNarrowingRequiresWidthAndStabilitySafetyMargins()
    {
        var eligible = new StabilitySafeRowNarrowingEstimate(128, 32, true, false);
        var small = eligible with { DroppedWidthBytes = 8 };
        var allocating = eligible with { AddsRowObjectAllocation = true };

        Assert.IsTrue(StabilitySafeRowNarrowingPolicy.CanNarrow(eligible));
        Assert.IsFalse(StabilitySafeRowNarrowingPolicy.CanNarrow(small));
        Assert.IsFalse(StabilitySafeRowNarrowingPolicy.CanNarrow(allocating));
    }

    [TestMethod]
    public void RecursiveInvariantFactsKeepStableAnchorValuesOutsideTheFixpoint()
    {
        var frontier = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "r" };
        var stable = RecursiveScalarInvariantFacts.Classify(
            new BinaryOp(
                BinaryOpKind.Add,
                new Literal(1, typeof(int)),
                new Literal(2, typeof(int)),
                typeof(int)),
            frontier);
        var frontierValue = RecursiveScalarInvariantFacts.Classify(
            new ColumnRef("r", "Value", typeof(int)),
            frontier,
            isAnchorInvariant: false);
        var aggregate = RecursiveScalarInvariantFacts.Classify(
            new AggregateRef("sum", typeof(int)),
            frontier);

        Assert.IsTrue(stable.CanHoist);
        Assert.AreEqual(RecursiveScalarInvariantKind.FrontierDependent, frontierValue.Kind);
        Assert.IsFalse(frontierValue.CanHoist);
        Assert.AreEqual(RecursiveScalarInvariantKind.Aggregate, aggregate.Kind);
        Assert.IsFalse(aggregate.CanHoist);
    }

    [TestMethod]
    public void RecursiveInvariantFactsRejectVolatileStreamsAndWindows()
    {
        var frontier = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Assert.AreEqual(
            RecursiveScalarInvariantKind.Volatile,
            RecursiveScalarInvariantFacts.Classify(
                new MethodCall(
                    typeof(ScalarReuseModelTests).GetMethod(
                        nameof(VolatileValue),
                        BindingFlags.Static | BindingFlags.NonPublic)!,
                    [],
                    null,
                    typeof(int)),
                frontier).Kind);
        Assert.AreEqual(
            RecursiveScalarInvariantKind.Stream,
            RecursiveScalarInvariantFacts.Classify(new CteTableRef("rows"), frontier).Kind);
        Assert.AreEqual(
            RecursiveScalarInvariantKind.Window,
            RecursiveScalarInvariantFacts.Classify(new WindowFunctionRef(0, typeof(int)), frontier).Kind);
    }

    [NonDeterministic]
    private static int VolatileValue() => 1;
}
