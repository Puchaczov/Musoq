using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record SkipNode(PagingCount Count, LogicalNode Input) : LogicalNode(Input.OutputSchema)
{
    public SkipNode(int count, LogicalNode input) : this((PagingCount)count, input)
    {
    }

    public override IReadOnlyList<LogicalNode> Children { get; } = [Input];
}
