using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceInteractionPlanningResult(
    IReadOnlyDictionary<string, SourceInteractionPlan> PlansBySourceId,
    IReadOnlyList<SourceBoundaryPlan> BoundaryPlans,
    IReadOnlyList<SourceBoundaryStrategyPlan> BoundaryStrategyPlans,
    IReadOnlyList<PlanningDecision> Decisions);
