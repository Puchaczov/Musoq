using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PredicateMovementPlan(
    string MovementId,
    JoinNode Join,
    PredicateMovementSide Side,
    PredicatePlacementOrigin Origin,
    string Alias,
    IrExpression Predicate,
    string PredicateText,
    PlanningConfidence Confidence,
    string Reason);
