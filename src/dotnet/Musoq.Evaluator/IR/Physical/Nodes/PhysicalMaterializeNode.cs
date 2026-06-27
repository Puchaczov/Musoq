using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalMaterializeNode(
    PhysicalNode Input) : PhysicalNode(Input.OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
