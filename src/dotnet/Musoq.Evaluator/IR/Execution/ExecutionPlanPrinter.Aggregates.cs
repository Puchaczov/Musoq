using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static string FormatAggregateCall(ExecutionAggregateCall aggregateCall)
    {
        if (!string.IsNullOrWhiteSpace(aggregateCall.DisplayName))
            return aggregateCall.DisplayName;

        if (aggregateCall.Arguments.Count == 0 &&
            aggregateCall.Accumulator is { Identifier.Length: > 0 } accumulator)
        {
            return $"{aggregateCall.Method.MethodName}('{accumulator.Identifier}')";
        }

        return $"{aggregateCall.Method.MethodName}({FormatAggregateArguments(aggregateCall.Arguments)})";
    }

    private static string FormatTypedAggregateSet(ExecutionAggregateSet aggregateSet)
    {
        var input = aggregateSet.AccumulatorInput == null
            ? string.Empty
            : FormatExpression(aggregateSet.AccumulatorInput);
        var owner = aggregateSet.Accumulator.OwnerFieldName is null
            ? aggregateSet.Group.Name
            : $"{aggregateSet.Group.Name}.{aggregateSet.Accumulator.OwnerFieldName}";
        var state = $"{owner}.{aggregateSet.Accumulator.FieldName}";
        var arguments = string.IsNullOrEmpty(input)
            ? state
            : $"{state}, {input}";
        var filter = aggregateSet.FilterPredicate == null
            ? string.Empty
            : $" filter {FormatExpression(aggregateSet.FilterPredicate)}";

        return $"TypedAggregateSet [Set({arguments}){filter}]";
    }

    private static string FormatCompositeKey(ExecutionCompositeKey compositeKey)
    {
        var builder = new StringBuilder("CompositeKey(");
        for (var index = 0; index < compositeKey.Parts.Count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append(FormatExpression(compositeKey.Parts[index]));
        }

        builder.Append(')');
        return builder.ToString();
    }

    private static string FormatAggregateArguments(IReadOnlyList<ExecutionExpression> arguments)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < arguments.Count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append(FormatExpression(arguments[index]));
        }

        return builder.ToString();
    }

    private static string FormatAggregateGroupType(AggregateGroupShape groupShape)
    {
        return groupShape.TypeName;
    }

    private static string FormatAggregateGroupShape(AggregateGroupShape groupShape)
    {
        return $"; typed: {groupShape.TypeName}";
    }

    private static string FormatTupleExpression(IReadOnlyList<ExecutionExpression> expressions)
    {
        return $"({string.Join(", ", expressions.Select(FormatExpression))})";
    }

    private static string FormatTupleType(IReadOnlyList<ExecutionTypeRef> types)
    {
        return $"({string.Join(", ", types.Select(FormatType))})";
    }
}
