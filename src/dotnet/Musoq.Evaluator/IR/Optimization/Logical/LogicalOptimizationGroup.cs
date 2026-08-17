using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal static class LogicalOptimizationGroup
{
    public static OptimizationPassPipeline<LogicalNode> Pipeline { get; } = new(
        OptimizationStage.LogicalOptimization,
        OptimizationPassRunMode.Once,
        [
            new(new DeadCteEliminationLogicalPass(), "Drop CTE definitions that no surviving node consumes.")
        ]);

    public static IReadOnlyList<IPlanOptimizationPass<LogicalNode>> Passes => Pipeline.Passes;
}

