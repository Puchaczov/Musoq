using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalHashJoinNode(
    JoinKind Kind,
    IrExpression[] BuildKeys,
    IrExpression[] ProbeKeys,
    IrExpression? Residual,
    PhysicalNode Left,
    PhysicalNode Right
) : PhysicalNode(JoinKindSemantics.SelectOutputSchema(Kind, Left.OutputSchema, Right.OutputSchema))
{
    internal GroupMaximumSummary? GroupMaximum { get; init; }

    public override IReadOnlyList<PhysicalNode> Children { get; } = [Left, Right];
}

internal sealed record GroupMaximumSummary(ColumnRef Probe, ColumnRef Build);
