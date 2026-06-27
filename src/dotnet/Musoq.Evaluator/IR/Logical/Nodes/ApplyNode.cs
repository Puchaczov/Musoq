using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record ApplyNode(
    ApplyKind Kind,
    LogicalNode Left,
    LogicalNode Right,
    bool WithOrdinality = false) : LogicalNode(Left.OutputSchema.Merge(Right.OutputSchema))
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Left, Right];
}
