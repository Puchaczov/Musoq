using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class SubqueryToCteNormalizationPass : IPlanOptimizationPass<RootNode>
{
    public string Name => "SubqueryToCteNormalization";

    public OptimizationResult<RootNode> Optimize(RootNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var before = plan.ToString();
        var subqueryRewriter = new SubqueryToCteRewriteVisitor();
        var subqueryTraverser = new SubqueryToCteRewriteTraverseVisitor(subqueryRewriter);
        plan.Accept(subqueryTraverser);

        var normalized = subqueryTraverser.Root;
        var changed = !string.Equals(before, normalized.ToString(), StringComparison.Ordinal);

        return changed
            ? OptimizationResult<RootNode>.Changed(
                normalized,
                CreateChangedReason(normalized))
            : OptimizationResult<RootNode>.NoChange(
                normalized,
                "No supported subquery forms required pre-logical normalization.");
    }

    private static string CreateChangedReason(RootNode normalized)
    {
        var facts = LogicalSubqueryOwnershipFactCollector.Collect(normalized);
        if (facts.Count == 0)
            return "Converted supported subquery forms into CTE/join-compatible pre-logical shapes.";

        var summary = string.Join(
            ", ",
            facts
                .GroupBy(static fact => fact.Kind)
                .OrderBy(static group => group.Key)
                .Select(static group => $"{group.Key}={group.Count()}"));

        return $"Converted supported subquery forms into CTE/join-compatible pre-logical shapes ({summary}).";
    }
}
