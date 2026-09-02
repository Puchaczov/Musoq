using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    private static ExecutionExpression ConvertColumnRef(
        ColumnRef column,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        var resolvedField = ResolveField(column, sourceShapes);
        if (resolvedField != null)
        {
            var (alias, field) = resolvedField.Value;

            if (field.AccessStrategy is RuntimeDynamicMemberPathAccess runtimePath)
            {
                ExecutionExpression current = new ExecutionFieldRead(
                    alias,
                    runtimePath.RootFieldName,
                    runtimePath.RootFieldType,
                    runtimePath.RootIsDynamic
                        ? new RuntimeDynamicMemberAccess(runtimePath.RootFieldName)
                        : new ClrPropertyAccess(runtimePath.RootFieldName));

                foreach (var segment in runtimePath.Segments)
                {
                    current = new ExecutionMemberRead(
                        current,
                        segment.MemberName,
                        segment.ResultType,
                        segment.IsDynamic);
                }

                return current;
            }

            return new ExecutionFieldRead(
                alias,
                field.Name,
                ExecutionClrBindingFactory.FromClr(column.ReturnType),
                field.AccessStrategy,
                field.GeneratedTypeName ??
                column.GeneratedTypeName ??
                (field.AccessStrategy as GeneratedRowNestedAccess)?.ValueTypeName)
            { Stability = field.Stability, SourceReadType = field.SourceReadType, EnumType = field.EnumType };
        }

        return new ExecutionFieldRead(
            column.Alias,
            column.ColumnName,
            ExecutionClrBindingFactory.FromClr(column.ReturnType),
            GeneratedTypeName: column.GeneratedTypeName)
        {
            Stability = column.Stability
        };
    }

    private static ResolvedExecutionField? ResolveField(
        ColumnRef column,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        if (!string.IsNullOrWhiteSpace(column.Alias))
        {
            if (sourceShapes.TryGetValue(column.Alias, out var shape))
                return ResolveField(column, shape);

            return ResolveFieldFromTransitionShape(column, sourceShapes.Values);
        }

        ResolvedExecutionField? resolved = null;
        foreach (var shape in sourceShapes.Values)
        {
            var candidate = ResolveField(column, shape);
            if (candidate == null)
                continue;
            if (resolved != null)
                return null;
            resolved = candidate;
        }

        return resolved;
    }

    private static ResolvedExecutionField? ResolveFieldFromTransitionShape(
        ColumnRef column,
        IEnumerable<RowShape> sourceShapes)
    {
        ResolvedExecutionField? resolved = null;
        foreach (var shape in sourceShapes.OfType<TableRowShape>())
        {
            var candidate = ResolveField(column, shape);
            if (candidate == null)
                continue;
            if (resolved != null)
                return null;
            resolved = candidate;
        }

        return resolved;
    }

    private static ResolvedExecutionField? ResolveField(ColumnRef column, RowShape sourceShape)
    {
        if (!RowShapeLookup.TryResolveSourceAlias(sourceShape, out var alias))
            return null;

        if (!string.IsNullOrWhiteSpace(column.Alias) &&
            !string.Equals(alias, column.Alias, StringComparison.OrdinalIgnoreCase) &&
            sourceShape is not TableRowShape)
            return null;

        var columnName = RemoveSourceAlias(column.ColumnName, column.Alias);
        var sourceRelativeColumnName = RemoveSourceAlias(columnName, alias);
        var qualifiedName = $"{alias}.{sourceRelativeColumnName}";
        var originalQualifiedName = string.IsNullOrWhiteSpace(column.Alias)
            ? columnName
            : $"{column.Alias}.{columnName}";
        var matchingFields = sourceShape.Fields
            .Where(candidate =>
                !(sourceRelativeColumnName.Contains('.', StringComparison.Ordinal) &&
                  candidate.Name.Contains('.', StringComparison.Ordinal) &&
                  candidate.AccessStrategy is RuntimeDynamicMemberAccess) &&
                (string.Equals(candidate.Name, columnName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(candidate.Name, column.ColumnName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(candidate.Name, sourceRelativeColumnName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(candidate.Name, originalQualifiedName, StringComparison.OrdinalIgnoreCase) ||
                 sourceRelativeColumnName.Contains('.', StringComparison.Ordinal) &&
                 HasQualifiedSuffix(candidate.Name, sourceRelativeColumnName) ||
                 string.Equals(candidate.QualifiedName, originalQualifiedName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(candidate.QualifiedName, qualifiedName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(candidate.QualifiedName, $"{alias}.{originalQualifiedName}", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        var field = matchingFields.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, columnName, StringComparison.Ordinal) ||
            string.Equals(candidate.Name, column.ColumnName, StringComparison.Ordinal) ||
            string.Equals(candidate.Name, sourceRelativeColumnName, StringComparison.Ordinal) ||
            string.Equals(candidate.Name, originalQualifiedName, StringComparison.Ordinal) ||
            string.Equals(candidate.QualifiedName, originalQualifiedName, StringComparison.Ordinal) ||
            string.Equals(candidate.QualifiedName, qualifiedName, StringComparison.Ordinal) ||
            string.Equals(candidate.QualifiedName, $"{alias}.{originalQualifiedName}", StringComparison.Ordinal)) ??
            matchingFields.FirstOrDefault();

        if (field != null)
            return new ResolvedExecutionField(alias, field);

        var nestedField = ExecutionFieldAccessResolver.ResolveNestedField(
            column,
            sourceShape,
            alias,
            sourceRelativeColumnName);
        if (nestedField != null)
            return nestedField;

        var indexedField = ExecutionFieldAccessResolver.ResolveIndexedField(
            column,
            sourceShape,
            alias,
            sourceRelativeColumnName);
        if (indexedField != null)
            return indexedField;

        var unqualifiedMatches = sourceShape.Fields
            .Where(candidate =>
                string.Equals(GetUnqualifiedFieldName(candidate.Name), column.ColumnName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetUnqualifiedFieldName(candidate.Name), sourceRelativeColumnName, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        field = unqualifiedMatches.Length == 1 ? unqualifiedMatches[0] : null;
        return field == null ? null : new ResolvedExecutionField(alias, field);
    }

    private static bool HasQualifiedSuffix(string candidate, string relativeName)
    {
        return candidate.EndsWith($".{relativeName}", StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveSourceAlias(string columnName, string sourceAlias)
    {
        var aliasPrefix = $"{sourceAlias}.";
        return columnName.StartsWith(aliasPrefix, StringComparison.OrdinalIgnoreCase)
            ? columnName[aliasPrefix.Length..]
            : columnName;
    }

    private static string GetUnqualifiedFieldName(string fieldName)
    {
        var separatorIndex = fieldName.LastIndexOf('.');
        return separatorIndex < 0 ? fieldName : fieldName[(separatorIndex + 1)..];
    }
}
