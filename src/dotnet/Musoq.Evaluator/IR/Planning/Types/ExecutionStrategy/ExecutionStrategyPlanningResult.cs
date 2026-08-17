using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record ExecutionStrategyPlanningResult(
    ExecutionStrategyPlan Strategies,
    IReadOnlyList<PlanningDecision> Decisions);
