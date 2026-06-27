using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record FilterNode(IrExpression Predicate, LogicalNode Input) : LogicalNode(Input.OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Input];
}
