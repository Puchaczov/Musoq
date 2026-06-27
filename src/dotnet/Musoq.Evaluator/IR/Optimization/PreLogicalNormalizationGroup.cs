using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Optimization;

internal static class PreLogicalNormalizationGroup
{
    public static OptimizationPassPipeline<RootNode> Pipeline { get; } = new(
        OptimizationStage.PreLogicalNormalization,
        OptimizationPassRunMode.Once,
        [
            new(new DistinctToGroupByNormalizationPass(), "Normalize DISTINCT into an equivalent GROUP BY."),
            new(new SubqueryToCteNormalizationPass(), "Normalize subqueries into CTE definitions.")
        ]);

    public static IReadOnlyList<IPlanOptimizationPass<RootNode>> Passes => Pipeline.Passes;
}
