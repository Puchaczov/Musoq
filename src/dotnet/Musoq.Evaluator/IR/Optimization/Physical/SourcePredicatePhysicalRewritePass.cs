using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.SourcePlanning;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class SourcePredicatePhysicalRewritePass : IPhysicalOptimizationPass
{
    public string Name => "SourcePredicatePhysicalRewrite";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = PhysicalOptimizationState.From(context);
        var rewritten = SourcePredicatePhysicalRewriter.Rewrite(
            plan,
            state.Facts.SourceRewrite.SourcePlanResultsBySourceId,
            state.Facts.SourceRewrite.SourcePredicatePlansBySourceId);

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(
                rewritten,
                "No accepted source predicate conjuncts were removed from the physical plan.")
            : OptimizationResult<PhysicalNode>.Changed(
                rewritten,
                "Removed source-accepted predicate conjuncts from physical filters.");
    }
}

