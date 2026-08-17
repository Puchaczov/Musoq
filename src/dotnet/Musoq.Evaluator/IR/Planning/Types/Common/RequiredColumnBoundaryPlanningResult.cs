using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record RequiredColumnBoundaryPlanningResult(
    IReadOnlyList<RequiredColumnBoundaryPlan> Plans,
    IReadOnlyList<PlanningDecision> Decisions);
