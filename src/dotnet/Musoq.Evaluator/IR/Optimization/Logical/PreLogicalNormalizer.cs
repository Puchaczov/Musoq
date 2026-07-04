using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed class PreLogicalNormalizer
{
    public PreLogicalNormalizationResult Normalize(RootNode initialRoot)
    {
        ArgumentNullException.ThrowIfNull(initialRoot);

        var trace = new OptimizationTrace();
        var result = new PlanOptimizationRunner<RootNode>(
            PreLogicalNormalizationGroup.Pipeline).Run(
            initialRoot,
            new OptimizationContext(OptimizationStage.PreLogicalNormalization, trace));

        return new PreLogicalNormalizationResult(
            initialRoot,
            result.Plan,
            trace,
            LogicalSubqueryOwnershipFactCollector.Collect(result.Plan));
    }
}

