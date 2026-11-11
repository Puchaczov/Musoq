using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class ByteBufferFlowPlanner
{
    private ByteBufferFlowBindings AnalyzeOrdinary(PhysicalNode node, FlowScope scope, ByteBufferFlowBindings correlated)
    {
        if (node is PhysicalInterpretSourceNode interpret)
            return AnalyzeInterpret(interpret, correlated);
        if (node is PhysicalSchemaScanNode scan)
        {
            foreach (var argument in scan.Arguments)
                Observe(argument, correlated);
            return ByteBufferFlowBindings.Source(scan.OutputSchema, scan.Alias);
        }
        var input = ByteBufferFlowBindings.Merge(node.Children.Select(child => Analyze(child, scope, correlated)));
        var expressions = node switch
        {
            PhysicalFilterNode filter => [filter.Predicate],
            PhysicalHavingFilterNode filter => [filter.Predicate],
            PhysicalQualifyFilterNode filter => [filter.Predicate],
            PhysicalSortNode sort => sort.Keys.Select(key => key.Expression),
            PhysicalTopNNode top => top.Keys.Select(key => key.Expression),
            PhysicalTopOffsetNode top => top.Keys.Select(key => key.Expression),
            PhysicalHashJoinNode join => join.BuildKeys.Concat(join.ProbeKeys).Concat(join.Residual is { } residual ? [residual] : []),
            PhysicalNestedLoopJoinNode join => join.OnPredicate is { } predicate ? [predicate] : [],
            PhysicalSkipNode or PhysicalTakeNode or PhysicalMaterializeNode => [],
            _ => null as System.Collections.Generic.IEnumerable<IrExpression>
        };
        if (expressions is null)
        {
            input.RequireComplete();
            correlated.RequireComplete();
            return new ByteBufferFlowBindings();
        }
        foreach (var expression in expressions)
            Observe(expression, input);
        return input;
    }

    private ByteBufferFlowBindings AnalyzeInterpret(PhysicalInterpretSourceNode source, ByteBufferFlowBindings input)
    {
        if (source.Arguments[0] is ColumnRef buffer && buffer.ReturnType == typeof(byte[]) &&
            source.Kind is not (InterpretSourceKind.Parse or InterpretSourceKind.TryParse or InterpretSourceKind.PartialParse))
        {
            var maximum = source.Kind == InterpretSourceKind.InterpretAt ? null : bounds?.MaximumBytes(source.SchemaName);
            Require(buffer, input, maximum.HasValue ? ByteBufferRequirement.Prefix(maximum.Value) : ByteBufferRequirement.Complete);
        }
        else
            Observe(source.Arguments[0], input);
        foreach (var argument in source.Arguments.Skip(1))
            Observe(argument, input);
        var output = ByteBufferFlowBindings.Source(source.OutputSchema, source.Alias);
        _boundaries.Add(source, new(input, output, []));
        return output;
    }
}
