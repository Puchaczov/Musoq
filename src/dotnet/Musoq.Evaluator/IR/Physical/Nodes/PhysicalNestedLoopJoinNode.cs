using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalNestedLoopJoinNode(
    JoinKind Kind,
    IrExpression OnPredicate,
    PhysicalNode Left,
    PhysicalNode Right,
    OrderField? TieBreak = null,
    bool WithOrdinality = false) : PhysicalNode(JoinKindSemantics.SelectOutputSchema(Kind, Left.OutputSchema, Right.OutputSchema))
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Left, Right];
}
