using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalAggregateCandidateNode(
    IrExpression[] GroupKeys,
    string[] GroupKeyNames,
    Type[] GroupKeyTypes,
    AggregateBinding[] Bindings,
    PhysicalNode Input) : PhysicalNode(OutputSchemaFactory.ForGroupedAggregate(
        GroupKeyNames,
        GroupKeyTypes,
        Bindings,
        AggregateOutputName.Identifier))
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];
}
