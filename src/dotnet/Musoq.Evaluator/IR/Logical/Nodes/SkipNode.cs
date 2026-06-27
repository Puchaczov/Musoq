using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record SkipNode(int Count, LogicalNode Input) : LogicalNode(Input.OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Input];
}
