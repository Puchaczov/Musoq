using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal static class ExecutionIrOptimizationGroup
{
    public static OptimizationPassPipeline<ExecutionPlan> Pipeline { get; } = new(
        OptimizationStage.ExecutionIrOptimization,
        OptimizationPassRunMode.Once,
        [
            new(new SingleUsePipelineFusionPass(), "Fuse single-use producer/consumer pipelines before later rewrites."),
            new(new CteReadOnceFusionPass(), "Fuse read-once CTE materializations into their single consumer."),
            new(new CteSidecarIndexLoweringPass(), "Lower CTE sidecar index lookups before target reuse."),
            new(new MethodTargetReusePass(), "Reuse method targets before CSE and capacity rewrites run."),
            new(new LoopInvariantCodeMotionPass(), "Hoist stable loop-invariant scalars before local field and expression CSE."),
            new(new FieldExpressionHoistingPass(), "Hoist repeated field reads into reusable locals."),
            new(new ExpressionCseHoistingPass(), "Hoist repeated deterministic expressions via common-subexpression elimination."),
            new(new CapacityHintPass(), "Attach capacity hints to materialization nodes."),
            new(new MethodTargetReusePass(), "Run again after CSE/capacity rewrites so reusable targets cover the expressions those rewrites introduce.")
        ]);

    public static IReadOnlyList<IPlanOptimizationPass<ExecutionPlan>> Passes => Pipeline.Passes;
}
