using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceBoundaryStrategyPlanningResult(
    IReadOnlyList<SourceBoundaryStrategyPlan> Plans,
    IReadOnlyList<PlanningDecision> Decisions);
