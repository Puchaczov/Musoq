using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalSortMergeJoinNode(
    JoinKind Kind,
    IrExpression LeftKey,
    IrExpression RightKey,
    BinaryOpKind ComparisonKind,
    IrExpression Residual,
    PhysicalNode Left,
    PhysicalNode Right) : PhysicalNode(JoinKindSemantics.SelectOutputSchema(Kind, Left.OutputSchema, Right.OutputSchema))
{
    public IrExpression[] LeftPartitionKeys { get; init; } = [];

    public IrExpression[] RightPartitionKeys { get; init; } = [];

    public override IReadOnlyList<PhysicalNode> Children { get; } = [Left, Right];
}
