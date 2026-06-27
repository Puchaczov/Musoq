using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalSingleKeyAggregateNode(
    IrExpression GroupKey,
    string GroupKeyName,
    Type GroupKeyType,
    AggregateBinding[] Bindings,
    PhysicalNode Input) : PhysicalNode(OutputSchemaFactory.ForSingleKeyAggregate(
        GroupKeyName,
        GroupKeyType,
        Bindings,
        AggregateOutputName.Identifier))
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
