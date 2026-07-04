using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SingleUsePipelineFusionPassTests
{
    [TestMethod]
    public void Optimize_WhenSelectedFusionCandidateExists_ShouldExpandRelatedPhaseAndBody()
    {
        var table = new ExecutionVariable("result", typeof(object));
        var candidate = new ExecutionSingleUsePipelineFusionCandidate(
            4,
            new ExecutionBlock([new ExecutionReturnTable(table)]));
        var plan = new ExecutionPlan("compiled", [], new ExecutionBlock([candidate]));

        var result = new SingleUsePipelineFusionPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));

        Assert.IsTrue(result.IsChanged);
        Assert.HasCount(2, result.Plan.Body.Nodes);
        Assert.AreEqual(4, ((ExecutionRelatedCtePhase)result.Plan.Body.Nodes[0]).TableIndex);
        Assert.AreSame(table, ((ExecutionReturnTable)result.Plan.Body.Nodes[1]).Table);
    }

    [TestMethod]
    public void Optimize_WhenNoCandidateExists_ShouldLeavePlanUnchanged()
    {
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock([new ExecutionReturnTable(new ExecutionVariable("result", typeof(object)))]));

        var result = new SingleUsePipelineFusionPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }
}
