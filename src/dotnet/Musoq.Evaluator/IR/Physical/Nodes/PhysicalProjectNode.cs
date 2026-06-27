using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalProjectNode(
    ProjectedField[] Fields,
    PhysicalNode Input) : PhysicalNode(OutputSchemaFactory.ForProjection(Fields))
{
    public bool IsDistinct { get; init; }

    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
