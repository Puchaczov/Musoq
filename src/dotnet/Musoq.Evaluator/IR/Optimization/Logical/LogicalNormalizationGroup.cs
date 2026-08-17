using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal static class LogicalNormalizationGroup
{
    public static OptimizationPassPipeline<LogicalNode> Pipeline { get; } = new(
        OptimizationStage.LogicalNormalization,
        OptimizationPassRunMode.Once,
        [
            new(new LogicalConstantFoldingPass(), "Fold constant expressions before downstream analysis."),
            new(new LogicalSourceAliasAnalysisPass(), "Record source alias usage for later planning.")
        ]);

    public static IReadOnlyList<IPlanOptimizationPass<LogicalNode>> Passes => Pipeline.Passes;
}

