using System.Globalization;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static string CreateAggregateGroupShapeSignature(AggregateGroupShape shape)
    {
        var builder = new StringBuilder();
        builder.Append("keys:");
        foreach (var key in shape.Keys)
        {
            builder
                .Append(key.FieldName)
                .Append(':')
                .Append(ExecutionExpressionFingerprint.ForAggregateType(key.Type))
                .Append(';');
        }

        builder.Append("|captured:");
        foreach (var capturedField in shape.CapturedFields)
        {
            builder
                .Append(capturedField.FieldName)
                .Append(':')
                .Append(ExecutionExpressionFingerprint.ForAggregateType(capturedField.Type))
                .Append(';');
        }

        builder.Append("|accumulators:");
        foreach (var accumulator in shape.Accumulators)
            ExecutionExpressionFingerprint.AppendAggregateAccumulator(builder, accumulator);

        builder.Append("|owners:");
        foreach (var owner in shape.OwnerFields)
        {
            builder
                .Append(owner.PrefixLength.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(owner.FieldName)
                .Append('{')
                .Append(CreateAggregateGroupShapeSignature(owner.Shape))
                .Append("};");
        }

        return builder.ToString();
    }

    private static string CreateBlockSignature(
        ExecutionBlock block,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var builder = new StringBuilder();
        foreach (var node in block.Nodes)
        {
            builder
                .Append(CreateNodeSignature(node, parallelAggregate))
                .Append('|');
        }

        return builder.ToString();
    }

    private static string CreateNodeSignature(
        ExecutionNode node,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        return node switch
        {
            ExecutionLet let => $"let:{let.Variable.Name}:{ExecutionExpressionFingerprint.ForAggregateType(let.Variable.Type)}={ExecutionExpressionFingerprint.ForParallelAggregate(let.Value, parallelAggregate)}",
            ExecutionAssign assign => $"assign:{ExecutionExpressionFingerprint.ForAggregateVariable(assign.Variable, parallelAggregate)}={ExecutionExpressionFingerprint.ForParallelAggregate(assign.Value, parallelAggregate)}",
            ExecutionIf branch => $"if:{ExecutionExpressionFingerprint.ForParallelAggregate(branch.Condition, parallelAggregate)}:{CreateBlockSignature(branch.Body, parallelAggregate)}",
            ExecutionAggregateSet aggregateSet => CreateAggregateSetSignature(aggregateSet, parallelAggregate),
            ExecutionAggregateCapturedValueSet capturedValueSet => $"captured-set:{ExecutionExpressionFingerprint.ForAggregateVariable(capturedValueSet.Group, parallelAggregate)}:{capturedValueSet.CapturedField.FieldName}:{ExecutionExpressionFingerprint.ForParallelAggregate(capturedValueSet.Value, parallelAggregate)}",
            _ => $"{node.GetType().FullName}:{node}"
        };
    }

    private static string CreateAggregateSetSignature(
        ExecutionAggregateSet aggregateSet,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var builder = new StringBuilder();
        builder
            .Append("aggregate-set:")
            .Append(ExecutionExpressionFingerprint.ForAggregateVariable(aggregateSet.Group, parallelAggregate))
            .Append(':');
        ExecutionExpressionFingerprint.AppendAggregateAccumulator(builder, aggregateSet.Accumulator);
        builder.Append(":input=");
        builder.Append(aggregateSet.AccumulatorInput is null
            ? "<null>"
            : ExecutionExpressionFingerprint.ForParallelAggregate(aggregateSet.AccumulatorInput, parallelAggregate));
        builder.Append(":args=");
        foreach (var argument in aggregateSet.Arguments)
        {
            builder
                .Append(ExecutionExpressionFingerprint.ForParallelAggregate(argument, parallelAggregate))
                .Append(';');
        }

        return builder.ToString();
    }
}
