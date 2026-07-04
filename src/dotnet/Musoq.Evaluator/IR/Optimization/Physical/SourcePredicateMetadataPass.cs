using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class SourcePredicateMetadataPass : IPhysicalOptimizationPass
{
    public string Name => "SourcePredicateMetadata";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = PhysicalOptimizationState.From(context);
        var rewritten = Rewrite(plan, state, ResolvePredicates);

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(plan, "No source predicate metadata was applied.")
            : OptimizationResult<PhysicalNode>.Changed(rewritten, "Applied source predicate metadata to physical scans.");
    }

    private static IrExpression[] ResolvePredicates(PhysicalSchemaScanNode scan, PhysicalOptimizationState state)
    {
        return !string.IsNullOrWhiteSpace(scan.SourceContextId) &&
               state.Facts.SourceRewrite.PushedPredicatesBySourceId.TryGetValue(scan.SourceContextId, out var predicates)
            ? predicates
            : [];
    }

    internal static PhysicalNode Rewrite(
        PhysicalNode node,
        PhysicalOptimizationState state,
        Func<PhysicalSchemaScanNode, PhysicalOptimizationState, IrExpression[]> resolvePredicates)
    {
        if (node is PhysicalSchemaScanNode scan)
        {
            var predicates = resolvePredicates(scan, state);
            return predicates.Length == 0 && scan.PushedPredicates.Length == 0
                ? scan
                : scan with { PushedPredicates = predicates };
        }

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => Rewrite(child, state, resolvePredicates));
    }
}

