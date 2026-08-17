using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class CteSidecarAndFusionPlannerTests
{
    [TestMethod]
    public void CteSidecarStoragePlanner_WhenAllSelectedSlotsAreStored_ShouldUseIndexOnlyStorage()
    {
        var planner = new CteSidecarStoragePlanner(useCteSidecarIndexes: true);
        var specs = new[]
        {
            Spec(CteSidecarIndexKind.Hash, 0),
            Spec(CteSidecarIndexKind.KeySet, 1)
        };
        var nodes = new ExecutionNode[]
        {
            Store(0, ExecutionCteSidecarIndexKind.Hash),
            Store(1, ExecutionCteSidecarIndexKind.KeySet),
            new ExecutionHashAdd(
                new ExecutionVariable("hash", typeof(object)),
                new ExecutionLiteral(1, typeof(int)),
                new ExecutionVariable("row", typeof(object), "CteRow"),
                typeof(int),
                typeof(object),
                "CteRow")
        };
        var classifications = new Dictionary<string, CteReferenceClassification>(StringComparer.OrdinalIgnoreCase)
        {
            ["cte"] = new("cte", 2, CteOutputFlags.None)
        };

        var decision = planner.CreateStorageDecision("cte", specs, classifications, resultSupported: true, nodes, "CteRow");
        var rewritten = planner.ApplyIndexOnlyStorage("cteTable", "CteRow", nodes, decision);

        Assert.IsFalse(decision.StoreRows);
        Assert.IsTrue(decision.KeepPayloadRows);
        var marker = Assert.IsInstanceOfType<ExecutionCteIndexOnlyStorageCandidate>(rewritten[0]);
        Assert.AreEqual("cteTable", marker.TableName);
        Assert.AreEqual("CteRow", marker.RowTypeName);
        Assert.IsTrue(marker.KeepPayloadRows);
    }

    [TestMethod]
    public void CteSidecarStoragePlanner_WhenASelectedSlotIsMissing_ShouldKeepRowStorage()
    {
        var planner = new CteSidecarStoragePlanner(useCteSidecarIndexes: true);
        var specs = new[]
        {
            Spec(CteSidecarIndexKind.Hash, 0),
            Spec(CteSidecarIndexKind.KeySet, 1)
        };
        var classifications = new Dictionary<string, CteReferenceClassification>(StringComparer.OrdinalIgnoreCase)
        {
            ["cte"] = new("cte", 2, CteOutputFlags.None)
        };

        var decision = planner.CreateStorageDecision(
            "cte",
            specs,
            classifications,
            resultSupported: true,
            [Store(0, ExecutionCteSidecarIndexKind.Hash)],
            "CteRow");

        Assert.IsTrue(decision.StoreRows);
        Assert.IsTrue(decision.KeepPayloadRows);
    }

    [TestMethod]
    public void SidecarJoinRuntimePlanner_WhenOperationsAreReady_ShouldHoistGuardsAndKeySetSteps()
    {
        var planner = new SidecarJoinRuntimePlanner(CreateStepBlock, CreateGuardBlock);
        var hashStep = Step(CteSidecarIndexKind.Hash, ordinal: 0);
        var guard = new SidecarJoinRuntimeGuard(
            new Literal(true, typeof(bool)),
            new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(["base"], StringComparer.OrdinalIgnoreCase),
            1);
        var keySetStep = Step(CteSidecarIndexKind.KeySet, ordinal: 2);
        var tail = new ExecutionLet(new ExecutionVariable("tail", typeof(int)), new ExecutionLiteral(0, typeof(int)));

        var body = planner.CreateRuntimeBody(
            [hashStep, guard, keySetStep],
            new TableRowShape("base", []),
            new ExecutionBlock([tail]));

        var names = body.Nodes
            .OfType<ExecutionLet>()
            .Select(static node => node.Variable.Name)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "guard1", "step2", "step0", "tail" }, names);
    }

    [TestMethod]
    public void SidecarJoinRuntimePlanner_WhenRequiredAliasesNeverBecomeActive_ShouldReturnNullSchedule()
    {
        var planner = new SidecarJoinRuntimePlanner(CreateStepBlock, CreateGuardBlock);

        var scheduled = planner.TryScheduleRuntimeOperations(
            [
                Step(CteSidecarIndexKind.Hash, ordinal: 0, requiredAlias: "missing"),
                Step(CteSidecarIndexKind.KeySet, ordinal: 1, requiredAlias: "alsoMissing")
            ],
            new TableRowShape("base", []));

        Assert.IsNull(scheduled);
    }

    [TestMethod]
    public void SingleUseHashBuildFusionPlanner_WhenProducerIsSourceBackedOrExpando_ShouldRejectFusionEligibility()
    {
        Assert.IsFalse(SingleUseHashBuildFusionPlanner.CanFuseProducerSource(
            new PhysicalCteRefNode("cte", "c", OutputSchema.Empty)));
        Assert.IsFalse(SingleUseHashBuildFusionPlanner.CanFuseProducerShape(
            new ExpandoAdapterShape("x", "ExpandoRow", typeof(object), [])));
    }

    [TestMethod]
    public void SingleUseHashBuildFusionPlanner_WhenProducerIsFlatSourceWithGeneratedShape_ShouldAllowFusionEligibility()
    {
        Assert.IsTrue(SingleUseHashBuildFusionPlanner.CanFuseProducerSource(
            new PhysicalSchemaScanNode("schema", "table", [], "s", [], [], OutputSchema.Empty)));
        Assert.IsTrue(SingleUseHashBuildFusionPlanner.CanFuseProducerSource(
            new PhysicalValuesScanNode("v", [], OutputSchema.Empty)));
        Assert.IsTrue(SingleUseHashBuildFusionPlanner.CanFuseProducerShape(
            new GeneratedRowShape("GeneratedRow0", [])));
    }

    [TestMethod]
    public void SingleUseHashBuildFusionPlanner_WhenMatchedBodyReadsSubset_ShouldPrunePayloadFieldsAndValues()
    {
        var planner = new SingleUseHashBuildFusionPlanner();
        var payloadShape = new HashPayloadShape(
            "PayloadRow",
            [Field("A", 0), Field("B", 1), Field("C", 2)],
            []);
        var payload = new FusedHashPayload(
            payloadShape,
            [
                new ExecutionRowValue("A", new ExecutionLiteral(1, typeof(int))),
                new ExecutionRowValue("B", new ExecutionLiteral(2, typeof(int))),
                new ExecutionRowValue("C", new ExecutionLiteral(3, typeof(int)))
            ]);
        var matchedBody = new ExecutionBlock(
        [
            new ExecutionLet(
                new ExecutionVariable("value", typeof(int)),
                new ExecutionFieldRead("payload", "B", typeof(int), new GeneratedFieldAccess("B")))
        ]);

        var pruned = planner.TryPruneFusedHashPayload(
            payload,
            [payloadShape, new TableRowShape("payload", payloadShape.Fields)],
            matchedBody,
            "payload",
            out var result);

        Assert.IsTrue(pruned);
        Assert.IsNotNull(result);
        Assert.HasCount(1, result.Payload.Shape.Fields);
        Assert.AreEqual("B", result.Payload.Shape.Fields[0].Name);
        Assert.HasCount(1, result.Payload.Values);
        Assert.AreEqual("B", result.Payload.Values[0].FieldName);
        var rewrittenPayloadShape = Assert.IsInstanceOfType<HashPayloadShape>(result.Shapes[0]);
        Assert.HasCount(1, rewrittenPayloadShape.Fields);
        Assert.AreEqual("B", rewrittenPayloadShape.Fields[0].Name);
    }

    private static CteSidecarIndexSpec Spec(CteSidecarIndexKind kind, int slot) =>
        new("cte", kind, ["Id"], typeof(int), slot);

    private static ExecutionCteSidecarIndexStoreCandidate Store(int slot, ExecutionCteSidecarIndexKind kind) =>
        new(new ExecutionVariable($"index{slot}", typeof(object)), slot, kind, typeof(int));

    private static SidecarJoinRuntimeStep Step(
        CteSidecarIndexKind kind,
        int ordinal,
        string requiredAlias = "base") =>
        new(
            null!,
            Spec(kind, ordinal),
            null!,
            new ExecutionVariable($"index{ordinal}", typeof(object)),
            kind == CteSidecarIndexKind.Hash ? new ExecutionVariable($"matches{ordinal}", typeof(object)) : null,
            [],
            null,
            null,
            new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>([requiredAlias], StringComparer.OrdinalIgnoreCase),
            new HashSet<string>([$"introduced{ordinal}"], StringComparer.OrdinalIgnoreCase),
            ordinal);

    private static ExecutionBlock CreateStepBlock(SidecarJoinRuntimeStep step, ExecutionBlock body) =>
        new([new ExecutionLet(new ExecutionVariable($"step{step.Ordinal}", typeof(int)), new ExecutionLiteral(step.Ordinal, typeof(int))), ..body.Nodes]);

    private static ExecutionBlock CreateGuardBlock(SidecarJoinRuntimeGuard guard, ExecutionBlock body) =>
        new([new ExecutionLet(new ExecutionVariable($"guard{guard.Ordinal}", typeof(int)), new ExecutionLiteral(guard.Ordinal, typeof(int))), ..body.Nodes]);

    private static FieldBinding Field(string name, int index) =>
        new(name, name, index, typeof(int), FieldNullability.Unknown, new GeneratedFieldAccess(name));
}
