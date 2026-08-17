using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceInteractionPlanningResult(
    IReadOnlyDictionary<string, SourceInteractionPlan> PlansBySourceId,
    IReadOnlyList<SourceBoundaryPlan> BoundaryPlans,
    IReadOnlyList<SourceBoundaryStrategyPlan> BoundaryStrategyPlans,
    IReadOnlyList<PlanningDecision> Decisions);
