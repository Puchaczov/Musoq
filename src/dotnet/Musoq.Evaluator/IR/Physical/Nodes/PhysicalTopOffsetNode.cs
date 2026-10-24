using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalTopOffsetNode(
    PagingCount Skip,
    PagingCount Take,
    OrderField[] Keys,
    PhysicalNode Input) : PhysicalNode(Input.OutputSchema)
{
    public PhysicalTopOffsetNode(int skip, int take, OrderField[] keys, PhysicalNode input) : this((PagingCount)skip, (PagingCount)take, keys, input)
    {
    }

    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
