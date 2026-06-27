using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record AggregateNode(
    IrExpression[] GroupKeys,
    string[] GroupKeyNames,
    Type[] GroupKeyTypes,
    AggregateBinding[] Bindings,
    LogicalNode Input) : LogicalNode(OutputSchemaFactory.ForGroupedAggregate(
        GroupKeyNames,
        GroupKeyTypes,
        Bindings,
        AggregateOutputName.ColumnName))
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Input];
}
