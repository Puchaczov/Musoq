using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceBoundaryPlanningResult(
    IReadOnlyList<SourceBoundaryPlan> Plans,
    IReadOnlyList<SourceBoundaryStrategyPlan> StrategyPlans,
    IReadOnlyList<PlanningDecision> Decisions);
