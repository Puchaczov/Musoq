using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PredicatePlacementPlanningResult(
    IReadOnlyList<PredicatePlacementPlan> Plans,
    IReadOnlyList<PlanningDecision> Decisions);
