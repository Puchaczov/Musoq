using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record CteRefNode(
    string CteName,
    string Alias,
    OutputSchema OutputSchema) : LogicalNode(OutputSchema)
{
    internal IntermediateProducerId? IntermediateProducerId { get; init; }

    public override IReadOnlyList<LogicalNode> Children { get; } = Array.Empty<LogicalNode>();
}
