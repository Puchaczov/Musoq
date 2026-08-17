using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning.Subqueries;

internal sealed record SubqueryLoweringStrategyPlanningResult(
    IReadOnlyList<SubqueryLoweringStrategyDecision> Strategies,
    IReadOnlyList<PlanningDecision> Decisions);
