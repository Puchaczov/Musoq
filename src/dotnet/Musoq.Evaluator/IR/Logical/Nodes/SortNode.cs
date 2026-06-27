using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record SortNode(OrderField[] Keys, LogicalNode Input) : LogicalNode(Input.OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Input];
}
