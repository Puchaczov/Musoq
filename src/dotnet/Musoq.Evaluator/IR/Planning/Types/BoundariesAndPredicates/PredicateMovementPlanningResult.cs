using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PredicateMovementPlanningResult(
    IReadOnlyList<PredicateMovementPlan> Plans,
    IReadOnlyList<PlanningDecision> Decisions);
