using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record BoundaryRowShapePlanningResult(
    IReadOnlyList<BoundaryRowShapePlan> Plans,
    IReadOnlyList<PlanningDecision> Decisions);
