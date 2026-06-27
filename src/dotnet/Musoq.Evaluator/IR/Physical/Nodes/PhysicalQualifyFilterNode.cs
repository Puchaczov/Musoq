using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalQualifyFilterNode(
    IrExpression Predicate,
    PhysicalNode Input) : PhysicalNode(Input.OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
