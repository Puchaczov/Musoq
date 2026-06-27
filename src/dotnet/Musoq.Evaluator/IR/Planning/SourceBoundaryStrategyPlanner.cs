using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceBoundaryStrategyPlanner
{
    public static SourceBoundaryStrategyPlanningResult Plan(IReadOnlyList<SourceBoundaryPlan> boundaryPlans)
    {
        ArgumentNullException.ThrowIfNull(boundaryPlans);
        var plans = boundaryPlans.Select(CreateStrategyPlan).ToArray();
        var decisions = plans.Select(CreateDecision).ToArray();

        return new SourceBoundaryStrategyPlanningResult(plans, decisions);
    }

    private static SourceBoundaryStrategyPlan CreateStrategyPlan(SourceBoundaryPlan plan)
    {
        var strategy = ResolveStrategy(plan);

        return new SourceBoundaryStrategyPlan(
            plan.BoundaryId,
            plan.Kind,
            plan.ApplyKind,
            plan.InputMode,
            strategy,
            SourceBoundaryCachingDecision.NotApplied,
            ResolveConfidence(strategy),
            CreateReason(plan, strategy));
    }

    private static SourceBoundaryStrategyKind ResolveStrategy(SourceBoundaryPlan plan)
    {
        if (plan.InputMode == SourceBoundaryInputMode.Correlated ||
            plan.InvocationShape == SourceInvocationShape.PerRow ||
            plan.Cacheability == SourceCacheability.NotCacheable)
        {
            return SourceBoundaryStrategyKind.PerRowRequired;
        }

        if (plan is { InputMode: SourceBoundaryInputMode.Independent, InvocationShape: SourceInvocationShape.PerQuery, Cacheability: SourceCacheability.CacheCandidate })
        {
            return SourceBoundaryStrategyKind.PerQueryCandidateNotApplied;
        }

        return SourceBoundaryStrategyKind.UnknownBoundary;
    }

    private static PlanningConfidence ResolveConfidence(SourceBoundaryStrategyKind strategy)
    {
        return strategy switch
        {
            SourceBoundaryStrategyKind.PerRowRequired => PlanningConfidence.High,
            SourceBoundaryStrategyKind.PerQueryCandidateNotApplied => PlanningConfidence.Medium,
            _ => PlanningConfidence.Low
        };
    }

    private static PlanningDecision CreateDecision(SourceBoundaryStrategyPlan plan)
    {
        return new PlanningDecision(
            PlanningDecisionCategory.SourceBoundaryStrategy,
            "SourceBoundaryStrategyPlan",
            plan.BoundaryId,
            plan.Strategy.ToString(),
            plan.Confidence,
            plan.Reason);
    }

    private static string CreateReason(SourceBoundaryPlan plan, SourceBoundaryStrategyKind strategy)
    {
        return strategy switch
        {
            SourceBoundaryStrategyKind.PerRowRequired =>
                $"{plan.Kind} boundary {plan.BoundaryId} is correlated and must stay per-row; plugin/source calls remain uncached.",
            SourceBoundaryStrategyKind.PerQueryCandidateNotApplied =>
                $"{plan.Kind} boundary {plan.BoundaryId} is independent and is only a per-query candidate; source/plugin caching is not applied without a capability design.",
            _ =>
                $"{plan.Kind} boundary {plan.BoundaryId} has unknown dependency shape; the planner keeps the existing per-invocation behavior and applies no caching."
        };
    }
}
