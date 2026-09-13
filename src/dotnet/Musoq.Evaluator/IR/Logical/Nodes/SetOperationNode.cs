using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record SetOperationNode(
    SetOpKind Kind,
    LogicalNode Left,
    LogicalNode Right,
    string[] Keys) : LogicalNode(OutputSchemaFactory.ForSetOperation(Left.OutputSchema, Right.OutputSchema))
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Left, Right];
}
