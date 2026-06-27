using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceBoundaryPlanningResult(
    IReadOnlyList<SourceBoundaryPlan> Plans,
    IReadOnlyList<SourceBoundaryStrategyPlan> StrategyPlans,
    IReadOnlyList<PlanningDecision> Decisions);
