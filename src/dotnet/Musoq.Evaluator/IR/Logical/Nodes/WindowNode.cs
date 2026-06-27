using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record WindowNode(
    WindowRegistration[] Registrations,
    LogicalNode Input) : LogicalNode(OutputSchemaFactory.ForWindow(Input.OutputSchema, Registrations))
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Input];
}
