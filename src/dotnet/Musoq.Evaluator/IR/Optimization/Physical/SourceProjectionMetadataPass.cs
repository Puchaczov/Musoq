using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class SourceProjectionMetadataPass : IPhysicalOptimizationPass
{
    public string Name => "SourceProjectionMetadata";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = PhysicalOptimizationState.From(context);
        var rewritten = Rewrite(plan, state);

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(plan, "No source projection metadata was applied.")
            : OptimizationResult<PhysicalNode>.Changed(rewritten, "Applied source projection metadata to physical scans.");
    }

    private static PhysicalNode Rewrite(PhysicalNode node, PhysicalOptimizationState state)
    {
        if (node is PhysicalSchemaScanNode scan)
        {
            var projectedColumns = !string.IsNullOrWhiteSpace(scan.SourceContextId) &&
                                   state.Facts.SourceRewrite.ProjectedColumnsBySourceId.TryGetValue(scan.SourceContextId, out var columns)
                ? columns
                : [];
            return projectedColumns.Length == 0 && scan.ProjectedColumns.Length == 0
                ? scan
                : scan with { ProjectedColumns = projectedColumns };
        }

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => Rewrite(child, state));
    }
}

