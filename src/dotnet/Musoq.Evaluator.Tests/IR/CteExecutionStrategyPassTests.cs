using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class CteExecutionStrategyPassTests
{
    [TestMethod]
    public void CteReadOnceFusion_WhenSelectedCandidateExists_ShouldExpandRelatedPhaseAndBody()
    {
        var table = new ExecutionVariable("result", typeof(object));
        var candidate = new ExecutionCteReadOnceFusionCandidate(
            9,
            new ExecutionBlock([new ExecutionReturnTable(table)]));
        var plan = new ExecutionPlan("compiled", [], new ExecutionBlock([candidate]));

        var result = new CteReadOnceFusionPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));

        Assert.IsTrue(result.IsChanged);
        Assert.HasCount(2, result.Plan.Body.Nodes);
        Assert.AreEqual(9, ((ExecutionRelatedCtePhase)result.Plan.Body.Nodes[0]).TableIndex);
        Assert.AreSame(table, ((ExecutionReturnTable)result.Plan.Body.Nodes[1]).Table);
    }

    [TestMethod]
    public void CteSidecarIndexLowering_WhenStoreAndLoadCandidatesExist_ShouldLowerFinalOperations()
    {
        var index = new ExecutionVariable("cteHash", typeof(object));
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionCteSidecarIndexStoreCandidate(index, 3, ExecutionCteSidecarIndexKind.Hash, typeof(int), typeof(object), "PayloadRow"),
                new ExecutionCteSidecarIndexLoadCandidate(index, 3, ExecutionCteSidecarIndexKind.Hash, typeof(int), typeof(object), "PayloadRow")
            ]));

        var result = new CteSidecarIndexLoweringPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));

        Assert.IsTrue(result.IsChanged);
        var store = (ExecutionStoreCteIndex)result.Plan.Body.Nodes[0];
        var load = (ExecutionLoadCteIndex)result.Plan.Body.Nodes[1];

        Assert.AreSame(index, store.Index);
        Assert.AreEqual(3, store.IndexSlot);
        Assert.AreEqual(ExecutionCteSidecarIndexKind.Hash, store.Kind);
        Assert.AreSame(index, load.Index);
        Assert.AreEqual("PayloadRow", load.GeneratedRowTypeName);
    }

    [TestMethod]
    public void CteSidecarIndexLowering_WhenBuildAndAppendCandidatesExist_ShouldExpandFinalNodes()
    {
        var index = new ExecutionVariable("cteHash", typeof(object));
        var table = new ExecutionVariable("cte0", typeof(object));
        var rowShape = new GeneratedRowShape("PayloadRow", []);
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionCteSidecarIndexBuildCandidate(
                [
                    new ExecutionCteSidecarIndexCreateSpec(
                        index,
                        ExecutionCteSidecarIndexKind.Hash,
                        typeof(int),
                        null,
                        typeof(object),
                        "PayloadRow")
                ]),
                new ExecutionCteSidecarAppendRewriteCandidate(
                    new ExecutionAppendRow(table, rowShape, []),
                [
                    new ExecutionCteSidecarAppendIndexSpec(
                        index,
                        new ExecutionLiteral(1, typeof(int)),
                        ExecutionCteSidecarIndexKind.Hash,
                        typeof(int),
                        null,
                        [])
                ])
            ]));

        var result = new CteSidecarIndexLoweringPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));

        Assert.IsTrue(result.IsChanged);
        Assert.IsInstanceOfType<ExecutionCreateHash>(result.Plan.Body.Nodes[0]);
        Assert.IsInstanceOfType<ExecutionCreateGeneratedRow>(result.Plan.Body.Nodes[1]);
        Assert.IsInstanceOfType<ExecutionAppendExistingRow>(result.Plan.Body.Nodes[2]);
        Assert.IsInstanceOfType<ExecutionHashAdd>(result.Plan.Body.Nodes[3]);
    }

    [TestMethod]
    public void CteSidecarIndexLowering_WhenFusedProducerCandidateExists_ShouldCreateFinalProducer()
    {
        var table = new ExecutionVariable("cte0", typeof(object));
        var rowShape = new GeneratedRowShape("Cte0Row0", []);
        var output = new ExecutionFusedCteOutput(0, table, rowShape, StoreRows: false);
        var plan = new ExecutionPlan(
            "compiled",
            [rowShape],
            new ExecutionBlock(
            [
                new ExecutionCteFusedProducerCandidate(
                    [output],
                    new ExecutionBlock([new ExecutionReturnTable(table)]))
            ]));

        var result = new CteSidecarIndexLoweringPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));

        Assert.IsTrue(result.IsChanged);
        var producer = (ExecutionFusedCteProducer)result.Plan.Body.Nodes[0];
        Assert.AreSame(output, producer.Outputs[0]);
        Assert.IsInstanceOfType<ExecutionReturnTable>(producer.Body.Nodes[0]);
    }

    [TestMethod]
    public void CteSidecarIndexLowering_WhenIndexOnlyStorageCandidateExists_ShouldPruneRowStorage()
    {
        var table = new ExecutionVariable("cte0", typeof(object));
        var row = new ExecutionVariable("cte0Row", typeof(object), "Cte0Row0");
        var rowShape = new GeneratedRowShape("Cte0Row0", []);
        var plan = new ExecutionPlan(
            "compiled",
            [rowShape],
            new ExecutionBlock(
            [
                new ExecutionCteIndexOnlyStorageCandidate(table.Name, rowShape.TypeName, KeepPayloadRows: false),
                new ExecutionCreateTable(table, rowShape),
                new ExecutionCreateGeneratedRow(row, rowShape, [], []),
                new ExecutionAppendExistingRow(table, row)
            ]));

        var result = new CteSidecarIndexLoweringPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));

        Assert.IsTrue(result.IsChanged);
        Assert.IsEmpty(result.Plan.Body.Nodes);
        Assert.IsEmpty(result.Plan.Shapes);
    }

    [TestMethod]
    public void CtePasses_WhenNoCandidatesExist_ShouldLeavePlanUnchanged()
    {
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock([new ExecutionReturnTable(new ExecutionVariable("result", typeof(object)))]));

        var readOnce = new CteReadOnceFusionPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));
        var sidecar = new CteSidecarIndexLoweringPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));

        Assert.IsFalse(readOnce.IsChanged);
        Assert.AreSame(plan, readOnce.Plan);
        Assert.IsFalse(sidecar.IsChanged);
        Assert.AreSame(plan, sidecar.Plan);
    }
}
