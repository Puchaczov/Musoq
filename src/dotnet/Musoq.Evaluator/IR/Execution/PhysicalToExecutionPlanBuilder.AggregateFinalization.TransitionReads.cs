using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static ExecutionExpression ConvertProjectedExpression(
        ProjectedField field,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var expression = ExecutionExpressionConverter.Convert(field.Expression, sourceLookup);
        if (expression is not ExecutionFieldRead fieldRead ||
            fieldRead.AccessStrategy != null ||
            string.IsNullOrWhiteSpace(fieldRead.Alias))
        {
            return expression;
        }

        if (sourceLookup.TryGetValue(fieldRead.Alias, out var sourceShape) &&
            sourceShape is not TableRowShape)
        {
            return expression;
        }

        return TryCreateTransitionTableRead(field, sourceLookup, out var tableRead)
            ? tableRead
            : expression;
    }

    private static bool TryCreateTransitionTableRead(
        ProjectedField field,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        out ExecutionExpression expression)
    {
        var tableRows = sourceLookup.Values.OfType<TableRowShape>().ToArray();
        if (tableRows.Length != 1)
        {
            expression = new ExecutionLiteral((object?)null, field.Expression.ReturnType);
            return false;
        }

        var tableRow = tableRows[0];
        var binding = FindUniqueTransitionBinding(tableRow, field);

        if (binding == null)
        {
            var nestedRead = CreateNestedTransitionTableRead(field, tableRow);
            if (nestedRead != null)
            {
                expression = nestedRead;
                return true;
            }
        }

        if (binding == null && field.OutputIndex >= 0 && field.OutputIndex < tableRow.Fields.Count)
            binding = tableRow.Fields[field.OutputIndex];

        if (binding == null)
        {
            expression = new ExecutionLiteral((object?)null, field.Expression.ReturnType);
            return false;
        }

        expression = new ExecutionFieldRead(tableRow.Alias, binding.Name, field.Expression.ReturnType, binding.AccessStrategy);
        return true;
    }

    private static FieldBinding? FindUniqueTransitionBinding(
        TableRowShape tableRow,
        ProjectedField field)
    {
        var matches = tableRow.Fields
            .Where(candidate =>
                string.Equals(candidate.Name, field.OutputName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.QualifiedName, $"{tableRow.Alias}.{field.OutputName}", StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }

    private static ExecutionFieldRead? CreateNestedTransitionTableRead(
        ProjectedField field,
        TableRowShape tableRow)
    {
        if (field.Expression is not ColumnRef columnRef)
            return null;

        var columnPath = string.IsNullOrWhiteSpace(columnRef.Alias)
            ? columnRef.ColumnName
            : $"{columnRef.Alias}.{columnRef.ColumnName}";

        if (!columnPath.Contains('.', StringComparison.Ordinal))
            return null;

        var nestedBinding = FindNestedTransitionBinding(tableRow, columnPath);
        if (nestedBinding == null)
            return null;

        var (binding, propertyPath) = nestedBinding.Value;
        return new ExecutionFieldRead(
            tableRow.Alias,
            columnPath,
            field.Expression.ReturnType,
            new NestedPositionalAccess(binding.OutputIndex, propertyPath));
    }

    private static NestedTransitionBinding? FindNestedTransitionBinding(
        TableRowShape tableRow,
        string columnPath)
    {
        var segments = columnPath.Split('.');

        for (var prefixLength = segments.Length - 1; prefixLength >= 1; prefixLength--)
        {
            var prefix = CreateTransitionBindingPrefix(segments, prefixLength, out var indexerPath);
            var binding = tableRow.Fields.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, prefix, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.QualifiedName, $"{tableRow.Alias}.{prefix}", StringComparison.OrdinalIgnoreCase));

            if (binding == null)
                continue;

            var propertyPath = CreateTransitionPropertyPath(segments, prefixLength, indexerPath);
            if (string.IsNullOrWhiteSpace(propertyPath))
                continue;

            if (!CanReadNestedTransitionBinding(binding, propertyPath))
                continue;

            return new NestedTransitionBinding(binding, propertyPath);
        }

        return null;
    }

    private static string CreateTransitionBindingPrefix(
        string[] segments,
        int prefixLength,
        out string? indexerPath)
    {
        indexerPath = null;
        var prefixSegments = segments.Take(prefixLength).ToArray();
        var lastSegment = prefixSegments[^1];
        var indexerIndex = lastSegment.IndexOf('[', StringComparison.Ordinal);

        if (indexerIndex >= 0 && lastSegment.EndsWith(']'))
        {
            indexerPath = lastSegment[indexerIndex..];
            prefixSegments[^1] = lastSegment[..indexerIndex];
        }

        return string.Join('.', prefixSegments);
    }

    private static string CreateTransitionPropertyPath(
        string[] segments,
        int prefixLength,
        string? indexerPath)
    {
        var remainingPath = string.Join('.', segments.Skip(prefixLength));
        if (string.IsNullOrWhiteSpace(indexerPath))
            return remainingPath;

        return string.IsNullOrWhiteSpace(remainingPath)
            ? indexerPath
            : $"{indexerPath}.{remainingPath}";
    }

    private static bool CanReadNestedTransitionBinding(FieldBinding binding, string propertyPath)
    {
        if (binding.Type.ClrType == typeof(object))
            return true;

        if (propertyPath.Contains('[', StringComparison.Ordinal))
            return true;

        var currentType = binding.Type.ClrType;
        foreach (var segment in propertyPath.Split('.'))
        {
            currentType = Nullable.GetUnderlyingType(currentType) ?? currentType;

            var property = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return false;

            currentType = property.PropertyType;
        }

        return true;
    }

}
