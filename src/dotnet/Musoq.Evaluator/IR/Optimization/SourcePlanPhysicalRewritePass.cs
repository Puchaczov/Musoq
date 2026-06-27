using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.SourcePlanning;
using Musoq.Evaluator.IR.Planning;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class SourcePlanPhysicalRewritePass : IPlanOptimizationPass<PhysicalNode>
{
    public string Name => "SourcePlanPhysicalRewrite";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = PhysicalOptimizationState.From(context);
        var rewriteResult = SourcePlanPhysicalRewriter.Rewrite(
            plan,
            state.Properties.SourcePlanResultsBySourceId);
        state.Properties = state.Properties with
        {
            SourcePlanResultsBySourceId = rewriteResult.SourcePlanResultsBySourceId
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
