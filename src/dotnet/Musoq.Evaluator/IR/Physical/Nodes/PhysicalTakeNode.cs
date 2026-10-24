using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalTakeNode(
    PagingCount Count,
    PhysicalNode Input) : PhysicalNode(Input.OutputSchema)
{
    public PhysicalTakeNode(int count, PhysicalNode input) : this((PagingCount)count, input)
    {
    }

    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
