using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalCteRefNode(
    string CteName,
    string Alias,
    OutputSchema OutputSchema) : PhysicalNode(OutputSchema)
{
    internal IntermediateProducerId? IntermediateProducerId { get; init; }

    public override IReadOnlyList<PhysicalNode> Children { get; } = Array.Empty<PhysicalNode>();
}
