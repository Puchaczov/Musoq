using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalNestedLoopApplyNode(
    ApplyKind Kind,
    PhysicalNode Left,
    PhysicalNode Right,
    bool WithOrdinality = false) : PhysicalNode(Left.OutputSchema.Merge(Right.OutputSchema))
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Left, Right];
}
