using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalTopOffsetNode(
    int Skip,
    int Take,
    OrderField[] Keys,
    PhysicalNode Input) : PhysicalNode(Input.OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
