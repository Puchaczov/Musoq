using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalValuesScanNode(
    string Alias,
    IReadOnlyList<ValuesScanRow> Rows,
    OutputSchema OutputSchema) : PhysicalNode(OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = Array.Empty<PhysicalNode>();
}
