using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalWindowNode(
    WindowRegistration[] Registrations,
    PhysicalNode Input) : PhysicalNode(OutputSchemaFactory.ForWindow(Input.OutputSchema, Registrations))
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
