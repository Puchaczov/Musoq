using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceBoundaryStrategyPlanningResult(
    IReadOnlyList<SourceBoundaryStrategyPlan> Plans,
    IReadOnlyList<PlanningDecision> Decisions);
