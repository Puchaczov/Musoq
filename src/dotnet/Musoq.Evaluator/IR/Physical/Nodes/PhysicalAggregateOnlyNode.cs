using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalAggregateOnlyNode(
    AggregateBinding[] Bindings,
    PhysicalNode Input) : PhysicalNode(OutputSchemaFactory.ForAggregateOnly(Bindings, AggregateOutputName.Identifier))
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
