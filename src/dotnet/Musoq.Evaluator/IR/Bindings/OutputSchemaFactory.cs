using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Bindings;

internal static class OutputSchemaFactory
{
    public static OutputSchema ForProjection(IReadOnlyList<ProjectedField> fields)
    {
        var columns = new ColumnSchema[fields.Count];

        for (var i = 0; i < fields.Count; i++)
            columns[i] = new ColumnSchema(
                fields[i].OutputName,
                fields[i].Expression.ReturnType,
                i,
                intendedTypeName: null,
                sourceReadType: fields[i].Expression.ReturnType,
                enumType: fields[i].Expression.EnumType);

        return new OutputSchema(columns);
    }

    public static OutputSchema ForSetOperation(OutputSchema left, OutputSchema right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Columns.Length != right.Columns.Length)
            return left;

        ColumnSchema[]? columns = null;
        for (var i = 0; i < left.Columns.Length; i++)
        {
            var leftColumn = left.Columns[i];
            var rightColumn = right.Columns[i];
            var enumType = leftColumn.EnumType ?? rightColumn.EnumType;
            if (enumType == null)
                continue;

            columns ??= [..left.Columns];
            var carrierType = EnumScalarTypeFacts.GetCarrierType(enumType.UnderlyingKind);
            var type = IsNullableValueType(leftColumn.Type) || IsNullableValueType(rightColumn.Type)
                ? MakeNullable(carrierType)
                : carrierType;
            columns[i] = new ColumnSchema(
                leftColumn.Name,
                type,
                leftColumn.Index,
                intendedTypeName: leftColumn.IntendedTypeName,
                sourceReadType: type,
                enumType: enumType)
            {
                Stability = leftColumn.Stability
            };
        }

        return columns == null ? left : new OutputSchema(columns);
    }

    public static OutputSchema ForWindow(OutputSchema inputSchema, IReadOnlyList<WindowRegistration> registrations)
    {
        var inputColumns = inputSchema.Columns;
        var columns = new ColumnSchema[inputColumns.Length + registrations.Count];

        for (var i = 0; i < inputColumns.Length; i++)
            columns[i] = inputColumns[i];

        for (var i = 0; i < registrations.Count; i++)
        {
            var registration = registrations[i];
            var index = inputColumns.Length + i;
            columns[index] = new ColumnSchema($"__window_{registration.WindowIndex}", registration.ReturnType, index);
        }

        return new OutputSchema(columns);
    }

    public static OutputSchema ForGroupedAggregate(
        IReadOnlyList<string> groupKeyNames,
        IReadOnlyList<Type> groupKeyTypes,
        IReadOnlyList<IrExpression> groupKeys,
        IReadOnlyList<AggregateBinding> bindings,
        AggregateOutputName aggregateOutputName)
    {
        var columns = new ColumnSchema[groupKeyNames.Count + bindings.Count];
        var index = 0;

        for (var i = 0; i < groupKeyNames.Count; i++)
        {
            var groupKey = groupKeys[i];
            columns[index] = new ColumnSchema(
                groupKeyNames[i],
                groupKeyTypes[i],
                index++,
                intendedTypeName: null,
                sourceReadType: groupKeyTypes[i],
                enumType: groupKey.EnumType);
        }

        foreach (var binding in bindings)
            columns[index] = new ColumnSchema(GetAggregateColumnName(binding, aggregateOutputName), binding.ReturnType, index++);

        return new OutputSchema(columns);
    }

    public static OutputSchema ForSingleKeyAggregate(
        string groupKeyName,
        Type groupKeyType,
        IrExpression groupKey,
        IReadOnlyList<AggregateBinding> bindings,
        AggregateOutputName aggregateOutputName)
    {
        var columns = new ColumnSchema[1 + bindings.Count];
        columns[0] = new ColumnSchema(
            groupKeyName,
            groupKeyType,
            0,
            intendedTypeName: null,
            sourceReadType: groupKeyType,
            enumType: groupKey.EnumType);

        for (var i = 0; i < bindings.Count; i++)
            columns[i + 1] = new ColumnSchema(GetAggregateColumnName(bindings[i], aggregateOutputName), bindings[i].ReturnType, i + 1);

        return new OutputSchema(columns);
    }

    public static OutputSchema ForAggregateOnly(
        IReadOnlyList<AggregateBinding> bindings,
        AggregateOutputName aggregateOutputName)
    {
        var columns = new ColumnSchema[bindings.Count];

        for (var i = 0; i < bindings.Count; i++)
            columns[i] = new ColumnSchema(GetAggregateColumnName(bindings[i], aggregateOutputName), bindings[i].ReturnType, i);

        return new OutputSchema(columns);
    }

    public static OutputSchema ForStatements<TNode>(
        IReadOnlyList<TNode> statements,
        Func<TNode, OutputSchema> getOutputSchema)
    {
        return statements.Count == 0
            ? OutputSchema.Empty
            : getOutputSchema(statements[^1]);
    }

    private static string GetAggregateColumnName(AggregateBinding binding, AggregateOutputName aggregateOutputName)
    {
        return aggregateOutputName switch
        {
            AggregateOutputName.ColumnName => binding.ColumnName,
            AggregateOutputName.Identifier => binding.Identifier,
            _ => throw new ArgumentOutOfRangeException(nameof(aggregateOutputName), aggregateOutputName, null)
        };
    }

    private static bool IsNullableValueType(Type type) => Nullable.GetUnderlyingType(type) != null;

    private static Type MakeNullable(Type type)
    {
        return IsNullableValueType(type) || !type.IsValueType
            ? type
            : typeof(Nullable<>).MakeGenericType(type);
    }
}
