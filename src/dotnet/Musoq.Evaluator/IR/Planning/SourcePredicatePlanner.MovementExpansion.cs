using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourcePredicatePlanner
{
    public static SourcePredicatePlanningResult ExpandWithPredicateMovements(
        SourcePredicatePlanningResult planningResult,
        IReadOnlyDictionary<string, SourcePlanProperties> sources,
        IReadOnlyList<PredicateMovementPlan> predicateMovementPlans)
    {
        ArgumentNullException.ThrowIfNull(planningResult);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(predicateMovementPlans);
        if (predicateMovementPlans.Count == 0)
            return planningResult;

        var sourcesByAlias = sources.Values
            .GroupBy(static source => source.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        var plans = planningResult.PlansBySourceId.ToDictionary(
            static entry => entry.Key,
            static entry => entry.Value,
            StringComparer.Ordinal);
        var pushedPredicates = planningResult.PushedPredicatesBySourceId.ToDictionary(
            static entry => entry.Key,
            static entry => entry.Value,
            StringComparer.Ordinal);
        var decisions = new List<PlanningDecision>();
        var changed = false;

        foreach (var movement in predicateMovementPlans)
        {
            if (!sourcesByAlias.TryGetValue(movement.Alias, out var matchingSources) ||
                matchingSources.Length != 1)
            {
                continue;
            }

            var source = matchingSources[0];
            if (!SourcePredicateExpressionConverter.TryConvertPredicate(movement.Predicate, movement.Alias, out _))
            {
                decisions.Add(CreateMovementExpansionDecision(
                    source.SourceContextId,
                    "Skipped",
                    PlanningConfidence.Medium,
                    $"Moved predicate {movement.PredicateText} cannot be represented by the source predicate DTO."));
                continue;
            }

            var existingPredicates = pushedPredicates.TryGetValue(source.SourceContextId, out var existing)
                ? existing
                : [];
            if (existingPredicates.Any(predicate => Equals(predicate, movement.Predicate)))
            {
                decisions.Add(CreateMovementExpansionDecision(
                    source.SourceContextId,
                    "AlreadyAvailable",
                    PlanningConfidence.High,
                    $"Moved predicate {movement.PredicateText} was already part of the source predicate request."));
                continue;
            }

            var expandedPredicates = existingPredicates.Append(movement.Predicate).ToArray();
            pushedPredicates[source.SourceContextId] = expandedPredicates;
            plans[source.SourceContextId] = CreateExpandedPlan(
                plans.TryGetValue(source.SourceContextId, out var existingPlan) ? existingPlan : null,
                source,
                expandedPredicates,
                movement);
            decisions.Add(CreateMovementExpansionDecision(
                source.SourceContextId,
                "Pushed",
                PlanningConfidence.High,
                $"Added moved source-local predicate {movement.PredicateText} to the source predicate request; the original runtime predicate remains as a semantic guard."));
            changed = true;
        }

        if (!changed)
        {
            return planningResult with
            {
                Decisions = planningResult.Decisions.Concat(decisions).ToArray()
            };
        }

        return new SourcePredicatePlanningResult(
            plans,
            pushedPredicates,
            planningResult.Decisions.Concat(decisions).ToArray());
    }

    private static SourcePredicatePlan CreateExpandedPlan(
        SourcePredicatePlan? existingPlan,
        SourcePlanProperties source,
        IrExpression[] pushedPredicates,
        PredicateMovementPlan movement)
    {
        if (existingPlan != null)
        {
            return existingPlan with
            {
                PushedPredicates = pushedPredicates,
                Reason = $"{existingPlan.Reason} Added moved source-local predicate {movement.PredicateText}."
            };
        }

        return new SourcePredicatePlan(
            source.SourceContextId,
            source.Alias,
            new WhereNode(new BooleanNode(true)),
            pushedPredicates,
            $"Added moved source-local predicate {movement.PredicateText}; runtime predicate remains for full semantics.",
            PlanningConfidence.High);
    }

    private static PlanningDecision CreateMovementExpansionDecision(
        string sourceContextId,
        string outcome,
        PlanningConfidence confidence,
        string reason)
    {
        return new PlanningDecision(
            PlanningDecisionCategory.PredicatePushdown,
            "SourcePredicateMovementExpansion",
            sourceContextId,
            outcome,
            confidence,
            reason);
    }
}
