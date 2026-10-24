using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalTopNNode(
    PagingCount N,
    OrderField[] Keys,
    PhysicalNode Input) : PhysicalNode(Input.OutputSchema)
{
    public PhysicalTopNNode(int n, OrderField[] keys, PhysicalNode input) : this((PagingCount)n, keys, input)
    {
    }

    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
