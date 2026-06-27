using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.SourcePlanning;
using Musoq.Evaluator.IR.Planning;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class SourcePredicatePhysicalRewritePass : IPlanOptimizationPass<PhysicalNode>
{
    public string Name => "SourcePredicatePhysicalRewrite";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = PhysicalOptimizationState.From(context);
        var rewritten = SourcePredicatePhysicalRewriter.Rewrite(
            plan,
            state.Properties.SourcePlanResultsBySourceId,
            state.Properties.SourcePredicatePlansBySourceId);

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(
                rewritten,
                "No accepted source predicate conjuncts were removed from the physical plan.")
            : OptimizationResult<PhysicalNode>.Changed(
                rewritten,
                "Removed source-accepted predicate conjuncts from physical filters.");
    }
}
