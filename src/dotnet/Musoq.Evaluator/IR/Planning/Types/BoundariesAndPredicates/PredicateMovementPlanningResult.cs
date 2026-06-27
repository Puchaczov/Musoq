using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PredicateMovementPlanningResult(
    IReadOnlyList<PredicateMovementPlan> Plans,
    IReadOnlyList<PlanningDecision> Decisions);
