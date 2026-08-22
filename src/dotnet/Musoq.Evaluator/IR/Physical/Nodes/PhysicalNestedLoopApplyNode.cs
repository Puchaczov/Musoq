using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalNestedLoopApplyNode(
    ApplyKind Kind,
    PhysicalNode Left,
    PhysicalNode Right,
    bool WithOrdinality = false) : PhysicalNode(Left.OutputSchema.Merge(Right.OutputSchema))
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Left, Right];

    internal IReadOnlyList<ApplyPredicateMovementPlan> ApplyPredicateMovementPlans { get; init; } = [];
}
