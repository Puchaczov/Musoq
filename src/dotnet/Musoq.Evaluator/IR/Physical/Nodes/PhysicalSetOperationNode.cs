using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalSetOperationNode(
    SetOpKind Kind,
    PhysicalNode Left,
    PhysicalNode Right,
    int[] FieldIndexes,
    Type[] FieldTypes) : PhysicalNode(OutputSchemaFactory.ForSetOperation(Left.OutputSchema, Right.OutputSchema))
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Left, Right];
}
