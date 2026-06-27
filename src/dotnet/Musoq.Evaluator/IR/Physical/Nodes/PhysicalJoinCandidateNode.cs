using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalJoinCandidateNode(
    JoinKind Kind,
    IrExpression OnPredicate,
    PhysicalNode Left,
    PhysicalNode Right,
    IrExpression[] LeftMovedPredicates,
    IrExpression[] RightMovedPredicates,
    OrderField? TieBreak = null) : PhysicalNode(JoinKindSemantics.SelectOutputSchema(Kind, Left.OutputSchema, Right.OutputSchema))
{
    public PhysicalJoinCandidateNode(
        JoinKind kind,
        IrExpression onPredicate,
        PhysicalNode left,
        PhysicalNode right)
        : this(kind, onPredicate, left, right, [], [], null)
    {
    }

    public override IReadOnlyList<PhysicalNode> Children { get; } = [Left, Right];
}
