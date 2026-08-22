using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Physical;

public sealed partial class PhysicalPlanBuilder
{
    private PhysicalNode LowerApply(ApplyNode apply, PhysicalStrategyPlan strategyPlan)
    {
        return new PhysicalNestedLoopApplyNode(apply.Kind, Lower(apply.Left, strategyPlan), Lower(apply.Right, strategyPlan), apply.WithOrdinality)
        {
            ApplyPredicateMovementPlans = ResolveApplyPredicateMovements(apply)
        };
    }

    private static Dictionary<ApplyNode, ApplyPredicateMovementPlan[]> CreateApplyPredicateMovementsByApply(
        IReadOnlyList<ApplyPredicateMovementPlan>? plans)
    {
        if (plans is null || plans.Count == 0)
            return new Dictionary<ApplyNode, ApplyPredicateMovementPlan[]>(ReferenceComparer<ApplyNode>.Instance);

        return plans.GroupBy(static plan => plan.Apply, ReferenceComparer<ApplyNode>.Instance).ToDictionary(
            static group => group.Key,
            static group => group.OrderBy(static plan => plan.MovementId, StringComparer.Ordinal)
                .GroupBy(static plan => plan.PredicateText, StringComparer.Ordinal)
                .Select(static group => group.First()).ToArray(),
            ReferenceComparer<ApplyNode>.Instance);
    }

    private IReadOnlyList<ApplyPredicateMovementPlan> ResolveApplyPredicateMovements(ApplyNode apply)
    {
        return _applyPredicateMovementsByApply.TryGetValue(apply, out var plans) ? plans : [];
    }
}
