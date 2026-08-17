using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record RowWidthPruningPlanningResult(
    IReadOnlyList<RowWidthPruningPlan> Plans,
    IReadOnlyList<PlanningDecision> Decisions);
