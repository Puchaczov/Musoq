using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record JoinNode(
    JoinKind Kind,
    IrExpression OnPredicate,
    LogicalNode Left,
    LogicalNode Right,
    OrderField? TieBreak = null,
    bool WithOrdinality = false) : LogicalNode(JoinKindSemantics.SelectOutputSchema(Kind, Left.OutputSchema, Right.OutputSchema))
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Left, Right];
}
