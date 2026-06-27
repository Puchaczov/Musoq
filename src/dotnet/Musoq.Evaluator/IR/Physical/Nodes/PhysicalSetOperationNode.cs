using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalSetOperationNode(
    SetOpKind Kind,
    PhysicalNode Left,
    PhysicalNode Right,
    int[] FieldIndexes,
    Type[] FieldTypes) : PhysicalNode(Left.OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Left, Right];
}
