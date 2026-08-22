using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record ApplyPredicateMovementPlan(
    string MovementId,
    ApplyNode Apply,
    PredicatePlacementOrigin Origin,
    PredicateEarliestPlacement Placement,
    string[] Aliases,
    IrExpression Predicate,
    string PredicateText,
    PlanningConfidence Confidence,
    string Reason)
{
    public string ResidualPredicateText { get; init; } = PredicateText;
}
