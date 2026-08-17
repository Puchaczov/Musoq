using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PhysicalStrategyPlanningResult(
    PhysicalStrategyPlan Strategies,
    IReadOnlyList<PlanningDecision> Decisions);
