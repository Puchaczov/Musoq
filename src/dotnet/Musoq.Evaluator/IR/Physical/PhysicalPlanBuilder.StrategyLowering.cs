using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;


namespace Musoq.Evaluator.IR.Physical;

public sealed partial class PhysicalPlanBuilder
{
    private PhysicalNode LowerTake(TakeNode node, PhysicalStrategyPlan strategyPlan)
    {
        return new PhysicalTakeNode(node.Count, Lower(node.Input, strategyPlan));
    }

    private PhysicalNode LowerAggregate(AggregateNode node, PhysicalStrategyPlan strategyPlan)
    {
        var input = Lower(node.Input, strategyPlan);

        return new PhysicalAggregateCandidateNode(
            node.GroupKeys,
            node.GroupKeyNames,
            node.GroupKeyTypes,
            node.Bindings,
            input);
    }

    private PhysicalNode LowerJoin(JoinNode node, PhysicalStrategyPlan strategyPlan)
    {
        var left = Lower(node.Left, strategyPlan);
        var right = Lower(node.Right, strategyPlan);
        return new PhysicalJoinCandidateNode(
            node.Kind,
            node.OnPredicate,
            left,
            right,
            ResolvePredicateMovements(node, PredicateMovementSide.Left),
            ResolvePredicateMovements(node, PredicateMovementSide.Right),
            node.TieBreak);
    }
    private static Dictionary<JoinNode, PredicateMovementPlan[]> CreatePredicateMovementsByJoin(
        IReadOnlyList<PredicateMovementPlan>? predicateMovementPlans)
    {
        if (predicateMovementPlans is null || predicateMovementPlans.Count == 0)
            return new Dictionary<JoinNode, PredicateMovementPlan[]>(ReferenceComparer<JoinNode>.Instance);

        return predicateMovementPlans
            .GroupBy(static plan => plan.Join, ReferenceComparer<JoinNode>.Instance)
            .ToDictionary(
                static group => group.Key,
                static group => group.OrderBy(static plan => plan.MovementId, StringComparer.Ordinal).ToArray(),
                ReferenceComparer<JoinNode>.Instance);
    }

    private IrExpression[] ResolvePredicateMovements(JoinNode join, PredicateMovementSide side)
    {
        if (!_predicateMovementsByJoin.TryGetValue(join, out var plans))
            return [];

        var predicates = new List<IrExpression>();
        for (var i = plans.Length - 1; i >= 0; i--)
        {
            var plan = plans[i];
            if (plan.Side != side)
                continue;

            predicates.Add(plan.Predicate);
        }

        return [.. predicates];
    }
}
