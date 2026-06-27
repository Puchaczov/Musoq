using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class OptimizationPassPipelineGuardrailTests
{
    [TestMethod]
    public void EveryPipeline_ShouldCarryItsStageAndRunMode()
    {
        Assert.AreEqual(OptimizationStage.PreLogicalNormalization, PreLogicalNormalizationGroup.Pipeline.Stage);
        Assert.AreEqual(OptimizationStage.LogicalNormalization, LogicalNormalizationGroup.Pipeline.Stage);
        Assert.AreEqual(OptimizationStage.LogicalOptimization, LogicalOptimizationGroup.Pipeline.Stage);
        Assert.AreEqual(OptimizationStage.PhysicalOptimization, PhysicalOptimizationGroup.Pipeline.Stage);
        Assert.AreEqual(OptimizationStage.ExecutionIrOptimization, ExecutionIrOptimizationGroup.Pipeline.Stage);
        Assert.AreEqual(OptimizationStage.CodegenReadability, CodegenReadabilityGroup.Pipeline.Stage);

        Assert.AreEqual(OptimizationPassRunMode.Once, PreLogicalNormalizationGroup.Pipeline.RunMode);
        Assert.AreEqual(OptimizationPassRunMode.Once, LogicalNormalizationGroup.Pipeline.RunMode);
        Assert.AreEqual(OptimizationPassRunMode.Once, LogicalOptimizationGroup.Pipeline.RunMode);
        Assert.AreEqual(OptimizationPassRunMode.Once, PhysicalOptimizationGroup.Pipeline.RunMode);
        Assert.AreEqual(OptimizationPassRunMode.Once, ExecutionIrOptimizationGroup.Pipeline.RunMode);
        Assert.AreEqual(OptimizationPassRunMode.Once, CodegenReadabilityGroup.Pipeline.RunMode);
    }

    [TestMethod]
    public void EveryPipelineStep_ShouldDeclareANonEmptyReason()
    {
        var reasons = AllStepReasons();

        Assert.IsNotEmpty(reasons);
        Assert.IsEmpty(
            reasons.Where(string.IsNullOrWhiteSpace),
            "Every optimization pipeline step must declare a non-empty reason.");
    }

    [TestMethod]
    public void ExecutionIrPipeline_ShouldDocumentBothMethodTargetReuseRunsDistinctly()
    {
        var reuseReasons = ExecutionIrOptimizationGroup.Pipeline.Steps
            .Where(step => step.Name == "MethodTargetReuse")
            .Select(step => step.Reason)
            .ToArray();

        Assert.HasCount(2, reuseReasons);
        Assert.AreNotEqual(
            reuseReasons[0],
            reuseReasons[1],
            "The two intentional MethodTargetReusePass runs should document distinct reasons.");
    }

    [TestMethod]
    public void PipelinePasses_ShouldMatchPipelineSteps()
    {
        AssertPassesMatchSteps(PreLogicalNormalizationGroup.Pipeline);
        AssertPassesMatchSteps(LogicalNormalizationGroup.Pipeline);
        AssertPassesMatchSteps(LogicalOptimizationGroup.Pipeline);
        AssertPassesMatchSteps(PhysicalOptimizationGroup.Pipeline);
        AssertPassesMatchSteps(ExecutionIrOptimizationGroup.Pipeline);
        AssertPassesMatchSteps(CodegenReadabilityGroup.Pipeline);
    }

    private static void AssertPassesMatchSteps<TPlan>(OptimizationPassPipeline<TPlan> pipeline)
    {
        CollectionAssert.AreEqual(
            pipeline.Steps.Select(step => step.Pass).ToArray(),
            pipeline.Passes.ToArray(),
            $"Pipeline for stage {pipeline.Stage} exposes passes that diverge from its declared steps.");
    }

    private static IReadOnlyList<string> AllStepReasons()
    {
        return
        [
            .. PreLogicalNormalizationGroup.Pipeline.Steps.Select(step => step.Reason),
            .. LogicalNormalizationGroup.Pipeline.Steps.Select(step => step.Reason),
            .. LogicalOptimizationGroup.Pipeline.Steps.Select(step => step.Reason),
            .. PhysicalOptimizationGroup.Pipeline.Steps.Select(step => step.Reason),
            .. ExecutionIrOptimizationGroup.Pipeline.Steps.Select(step => step.Reason),
            .. CodegenReadabilityGroup.Pipeline.Steps.Select(step => step.Reason)
        ];
    }
}
