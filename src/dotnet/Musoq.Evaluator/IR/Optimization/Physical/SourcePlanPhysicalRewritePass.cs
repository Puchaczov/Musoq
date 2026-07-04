using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.SourcePlanning;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class SourcePlanPhysicalRewritePass : IPhysicalOptimizationPass
{
    public string Name => "SourcePlanPhysicalRewrite";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = PhysicalOptimizationState.From(context);
        var rewriteResult = SourcePlanPhysicalRewriter.Rewrite(
            plan,
            state.Facts.SourceRewrite.SourcePlanResultsBySourceId);
        state.Facts = state.Facts with
        {
            SourceRewrite = state.Facts.SourceRewrite.WithSourcePlanResults(rewriteResult.SourcePlanResultsBySourceId)
        };

        return ReferenceEquals(plan, rewriteResult.PhysicalPlan)
            ? OptimizationResult<PhysicalNode>.NoChange(
                rewriteResult.PhysicalPlan,
                "No source-local order or slice operations were removed from the physical plan.")
            : OptimizationResult<PhysicalNode>.Changed(
                rewriteResult.PhysicalPlan,
                "Removed source-local order or slice operations accepted by source planning.");
    }
}

