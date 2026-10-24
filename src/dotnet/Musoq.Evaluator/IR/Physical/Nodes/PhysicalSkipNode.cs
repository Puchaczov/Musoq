using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalSkipNode(
    PagingCount Count,
    PhysicalNode Input) : PhysicalNode(Input.OutputSchema)
{
    public PhysicalSkipNode(int count, PhysicalNode input) : this((PagingCount)count, input)
    {
    }

    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
