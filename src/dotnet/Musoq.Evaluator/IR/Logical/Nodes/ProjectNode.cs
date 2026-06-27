using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record ProjectNode(ProjectedField[] Fields, LogicalNode Input) : LogicalNode(OutputSchemaFactory.ForProjection(Fields))
{
    public bool IsDistinct { get; init; }

    public override IReadOnlyList<LogicalNode> Children { get; } = [Input];
}
