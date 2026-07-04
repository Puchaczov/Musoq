using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed class DistinctToGroupByNormalizationPass : IPreLogicalNormalizationPass
{
    public string Name => "DistinctToGroupByNormalization";

    public OptimizationResult<RootNode> Optimize(RootNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var before = plan.ToString();

        var distinctRewriter = new DistinctToGroupByVisitor();
        var distinctTraverser = new DistinctToGroupByTraverseVisitor(distinctRewriter);
        plan.Accept(distinctTraverser);
        var normalized = distinctTraverser.Root;
        var changed = !string.Equals(before, normalized.ToString(), StringComparison.Ordinal);

        return changed
            ? OptimizationResult<RootNode>.Changed(
                normalized,
                "Converted DISTINCT query forms into pre-logical GROUP BY-compatible shapes.")
            : OptimizationResult<RootNode>.NoChange(
                normalized,
                "No DISTINCT query forms required pre-logical normalization.");
    }
}

