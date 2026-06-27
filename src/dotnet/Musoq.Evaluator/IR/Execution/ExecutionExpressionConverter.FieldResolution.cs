using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    private static ExecutionFieldRead ConvertColumnRef(
        ColumnRef column,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        var resolvedField = ResolveField(column, sourceShapes);
        if (resolvedField != null)
        {
            var (alias, field) = resolvedField.Value;
            return new ExecutionFieldRead(alias, field.Name, column.ReturnType, field.AccessStrategy);
        }

        return new ExecutionFieldRead(column.Alias, column.ColumnName, column.ReturnType);
    }

    private static ResolvedField? ResolveField(
        ColumnRef column,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        if (!string.IsNullOrWhiteSpace(column.Alias))
        {
            if (sourceShapes.TryGetValue(column.Alias, out var shape))
                return ResolveField(column, shape);

            return ResolveFieldFromTransitionShape(column, sourceShapes.Values);
        }

        ResolvedField? resolved = null;

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

    private static ResolvedField? ResolveFieldFromTransitionShape(
        ColumnRef column,
        IEnumerable<RowShape> sourceShapes)
    {
        ResolvedField? resolved = null;

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

    private static ResolvedField? ResolveField(ColumnRef column, RowShape sourceShape)
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
        var field = sourceShape.Fields.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, columnName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, column.ColumnName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, sourceRelativeColumnName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, originalQualifiedName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, originalQualifiedName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, qualifiedName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, $"{alias}.{originalQualifiedName}", StringComparison.OrdinalIgnoreCase));

        if (field != null) return new ResolvedField(alias, field);

        var nestedField = ResolveNestedField(column, sourceShape, alias, sourceRelativeColumnName);
        if (nestedField != null)
            return nestedField;

        var indexedField = ResolveIndexedField(column, sourceShape, alias, sourceRelativeColumnName);
        if (indexedField != null)
            return indexedField;

        var unqualifiedMatches = sourceShape.Fields
            .Where(candidate =>
                string.Equals(GetUnqualifiedFieldName(candidate.Name), column.ColumnName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetUnqualifiedFieldName(candidate.Name), sourceRelativeColumnName, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();

        field = unqualifiedMatches.Length == 1
            ? unqualifiedMatches[0]
            : null;

        return field == null ? null : new ResolvedField(alias, field);
    }

    private static ResolvedField? ResolveNestedField(
        ColumnRef column,
        RowShape sourceShape,
        string alias,
        string sourceRelativeColumnName)
    {
        if (!sourceRelativeColumnName.Contains('.', StringComparison.Ordinal) &&
            !sourceRelativeColumnName.Contains('[', StringComparison.Ordinal))
        {
            return null;
        }

        var nestedRoot = FindNestedRootField(column, sourceShape, alias, sourceRelativeColumnName);
        if (nestedRoot == null)
            return null;

        if (sourceShape is TableRowShape && IsSelfNestedTransitionAlias(nestedRoot))
            return new ResolvedField(alias, nestedRoot.Field);

        var fieldName = sourceShape is TableRowShape
            ? RemoveSourceAlias(nestedRoot.FieldName, alias)
            : sourceRelativeColumnName;
        FieldAccessStrategy? accessStrategy = sourceShape switch
        {
            TableRowShape when nestedRoot.Field.AccessStrategy is GeneratedRowTypeAccess generatedRow =>
                new GeneratedRowNestedAccess(
                    generatedRow.TypeName,
                    generatedRow.FieldName,
                    nestedRoot.PropertyPath),
            TableRowShape => new NestedPositionalAccess(
                nestedRoot.Field.OutputIndex,
                nestedRoot.PropertyPath),
            SourceEntityShape when nestedRoot.Field.AccessStrategy is ReflectedMemberAccess => new ReflectedMemberAccess(sourceRelativeColumnName),
            SourceEntityShape source when IsDirectScalarSource(source) => new NestedClrPropertyAccess(nestedRoot.PropertyPath),
            SourceEntityShape => new NestedClrPropertyAccess(sourceRelativeColumnName),
            _ => null
        };

        if (accessStrategy == null)
            return null;

        var field = new FieldBinding(
            fieldName,
            sourceShape is TableRowShape
                ? $"{alias}.{fieldName}"
                : string.IsNullOrWhiteSpace(column.Alias)
                    ? sourceRelativeColumnName
                    : $"{column.Alias}.{sourceRelativeColumnName}",
            nestedRoot.Field.OutputIndex,
            column.ReturnType,
            nestedRoot.Field.Nullability,
            accessStrategy);

        return new ResolvedField(alias, field);
    }

    private static bool IsSelfNestedTransitionAlias(NestedRootField nestedRoot)
    {
        return !nestedRoot.Field.Name.Contains('.', StringComparison.Ordinal) &&
               string.Equals(nestedRoot.PropertyPath, nestedRoot.Field.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static NestedRootField? FindNestedRootField(
        ColumnRef column,
        RowShape sourceShape,
        string alias,
        string sourceRelativeColumnName)
    {
        var originalQualifiedName = string.IsNullOrWhiteSpace(column.Alias)
            ? column.ColumnName
            : $"{column.Alias}.{column.ColumnName}";

        return sourceShape.Fields
            .SelectMany(candidate => CreateNestedRootMatches(
                candidate,
                alias,
                sourceRelativeColumnName,
                originalQualifiedName))
            .OrderByDescending(candidate => candidate.PrefixLength)
            .FirstOrDefault();
    }

    private static IEnumerable<NestedRootField> CreateNestedRootMatches(
        FieldBinding candidate,
        string alias,
        string sourceRelativeColumnName,
        string originalQualifiedName)
    {
        foreach (var match in CreateNestedRootMatches(candidate, sourceRelativeColumnName, sourceRelativeColumnName))
            yield return match;

        foreach (var match in CreateNestedRootMatches(candidate, originalQualifiedName, originalQualifiedName))
            yield return match;

        foreach (var match in CreateNestedRootMatches(candidate, $"{alias}.{sourceRelativeColumnName}", sourceRelativeColumnName))
            yield return match;

        foreach (var match in CreateNestedRootMatches(candidate, $"{alias}.{originalQualifiedName}", originalQualifiedName))
            yield return match;
    }

    private static IEnumerable<NestedRootField> CreateNestedRootMatches(
        FieldBinding candidate,
        string columnName,
        string fieldName)
    {
        foreach (var prefix in CreateNestedRootPrefixes(candidate))
        {
            if (!IsNestedPrefix(columnName, prefix))
                continue;

            yield return new NestedRootField(
                candidate,
                fieldName,
                CreateNestedPropertyPath(columnName, prefix),
                prefix.Length);
        }
    }

    private static IEnumerable<string> CreateNestedRootPrefixes(FieldBinding candidate)
    {
        yield return candidate.Name;
        yield return candidate.QualifiedName;

        var aliasSeparatorIndex = candidate.Name.IndexOf('.', StringComparison.Ordinal);
        if (aliasSeparatorIndex >= 0 && aliasSeparatorIndex < candidate.Name.Length - 1)
            yield return candidate.Name[(aliasSeparatorIndex + 1)..];

        aliasSeparatorIndex = candidate.QualifiedName.IndexOf('.', StringComparison.Ordinal);
        if (aliasSeparatorIndex >= 0 && aliasSeparatorIndex < candidate.QualifiedName.Length - 1)
            yield return candidate.QualifiedName[(aliasSeparatorIndex + 1)..];
    }

    private static bool IsNestedPrefix(string columnName, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) ||
            !columnName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            columnName.Length <= prefix.Length)
        {
            return false;
        }

        var nextCharacter = columnName[prefix.Length];
        return nextCharacter is '.' or '[';
    }

    private static string CreateNestedPropertyPath(string columnName, string rootName)
    {
        var propertyPath = columnName[rootName.Length..];
        return propertyPath.StartsWith('.')
            ? propertyPath[1..]
            : propertyPath;
    }

    private static ResolvedField? ResolveIndexedField(
        ColumnRef column,
        RowShape sourceShape,
        string alias,
        string sourceRelativeColumnName)
    {
        if (!sourceRelativeColumnName.Contains('[', StringComparison.Ordinal))
            return null;

        var rootName = GetRootFieldName(sourceRelativeColumnName);
        var rootField = sourceShape.Fields.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, rootName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, $"{column.Alias}.{rootName}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, $"{alias}.{rootName}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, $"{column.Alias}.{rootName}", StringComparison.OrdinalIgnoreCase));

        if (rootField == null)
            return null;

        FieldAccessStrategy? accessStrategy = sourceShape switch
        {
            SourceEntityShape when rootField.AccessStrategy is ReflectedMemberAccess => new ReflectedMemberAccess(sourceRelativeColumnName),
            SourceEntityShape => new NestedClrPropertyAccess(sourceRelativeColumnName),
            _ => null
        };

        if (accessStrategy == null)
            return null;

        var field = new FieldBinding(
            sourceRelativeColumnName,
            string.IsNullOrWhiteSpace(column.Alias)
                ? sourceRelativeColumnName
                : $"{column.Alias}.{sourceRelativeColumnName}",
            rootField.OutputIndex,
            column.ReturnType,
            rootField.Nullability,
            accessStrategy);

        return new ResolvedField(alias, field);
    }

    private static bool IsDirectScalarSource(SourceEntityShape source)
    {
        return source.Fields is [{ AccessStrategy: DirectScalarValueAccess }];
    }

    private static string GetRootFieldName(string columnName)
    {
        var separatorIndex = columnName.IndexOf('.', StringComparison.Ordinal);
        var rootSegment = separatorIndex < 0 ? columnName : columnName[..separatorIndex];
        var indexerIndex = rootSegment.IndexOf('[', StringComparison.Ordinal);
        return indexerIndex < 0 ? rootSegment : rootSegment[..indexerIndex];
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

    private readonly record struct ResolvedField(string Alias, FieldBinding Field);

    private sealed record NestedRootField(
        FieldBinding Field,
        string FieldName,
        string PropertyPath,
        int PrefixLength);
}
