using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record ApplyPredicateMovementPlanningResult(
    IReadOnlyList<ApplyPredicateMovementPlan> Plans,
    IReadOnlyList<PlanningDecision> Decisions);
