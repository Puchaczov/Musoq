using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record SetOperationNode(
    SetOpKind Kind,
    LogicalNode Left,
    LogicalNode Right,
    string[] Keys) : LogicalNode(Left.OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Left, Right];
}
