using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal static class PlanningRowShapeLookup
{
    public static PlanningField? ResolveField(PlanningRowShape sourceShape, ColumnRef column)
    {
        if (!string.IsNullOrWhiteSpace(column.Alias) &&
            !string.Equals(sourceShape.Alias, column.Alias, StringComparison.OrdinalIgnoreCase) &&
            sourceShape.Kind != PlanningRowShapeKind.TableRow)
        {
            return null;
        }

        var columnName = RemoveSourceAlias(column.ColumnName, column.Alias);
        var sourceRelativeColumnName = RemoveSourceAlias(columnName, sourceShape.Alias);
        var qualifiedName = $"{sourceShape.Alias}.{sourceRelativeColumnName}";
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
            string.Equals(candidate.QualifiedName, $"{sourceShape.Alias}.{originalQualifiedName}", StringComparison.OrdinalIgnoreCase));

        if (field != null)
            return field;

        return ResolveNestedField(sourceShape, sourceRelativeColumnName, originalQualifiedName) ??
               ResolveIndexedField(sourceShape, column, sourceRelativeColumnName) ??
               ResolveUnqualifiedField(sourceShape, column, sourceRelativeColumnName);
    }

    private static PlanningField? ResolveNestedField(
        PlanningRowShape sourceShape,
        string sourceRelativeColumnName,
        string originalQualifiedName)
    {
        if (!sourceRelativeColumnName.Contains('.', StringComparison.Ordinal) &&
            !sourceRelativeColumnName.Contains('[', StringComparison.Ordinal))
        {
            return null;
        }

        return sourceShape.Fields
            .SelectMany(field => CreateNestedRootMatches(
                field,
                sourceShape.Alias,
                sourceRelativeColumnName,
                originalQualifiedName))
            .OrderByDescending(match => match.PrefixLength)
            .FirstOrDefault()
            ?.Field;
    }

    private static PlanningField? ResolveIndexedField(
        PlanningRowShape sourceShape,
        ColumnRef column,
        string sourceRelativeColumnName)
    {
        if (!sourceRelativeColumnName.Contains('[', StringComparison.Ordinal))
            return null;

        var rootName = GetRootFieldName(sourceRelativeColumnName);
        return sourceShape.Fields.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, rootName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, $"{column.Alias}.{rootName}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, $"{sourceShape.Alias}.{rootName}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, $"{column.Alias}.{rootName}", StringComparison.OrdinalIgnoreCase));
    }

    private static PlanningField? ResolveUnqualifiedField(
        PlanningRowShape sourceShape,
        ColumnRef column,
        string sourceRelativeColumnName)
    {
        var matches = sourceShape.Fields
            .Where(candidate =>
                string.Equals(GetUnqualifiedFieldName(candidate.Name), column.ColumnName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetUnqualifiedFieldName(candidate.Name), sourceRelativeColumnName, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }

    private static IEnumerable<NestedRootField> CreateNestedRootMatches(
        PlanningField candidate,
        string alias,
        string sourceRelativeColumnName,
        string originalQualifiedName)
    {
        foreach (var match in CreateNestedRootMatches(candidate, sourceRelativeColumnName))
            yield return match;

        foreach (var match in CreateNestedRootMatches(candidate, originalQualifiedName))
            yield return match;

        foreach (var match in CreateNestedRootMatches(candidate, $"{alias}.{sourceRelativeColumnName}"))
            yield return match;

        foreach (var match in CreateNestedRootMatches(candidate, $"{alias}.{originalQualifiedName}"))
            yield return match;
    }

    private static IEnumerable<NestedRootField> CreateNestedRootMatches(
        PlanningField candidate,
        string columnName)
    {
        foreach (var prefix in CreateNestedRootPrefixes(candidate))
        {
            if (!IsNestedPrefix(columnName, prefix))
                continue;

            yield return new NestedRootField(candidate, prefix.Length);
        }
    }

    private static IEnumerable<string> CreateNestedRootPrefixes(PlanningField candidate)
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

        return columnName[prefix.Length] is '.' or '[';
    }

    private static string RemoveSourceAlias(string columnName, string sourceAlias)
    {
        var aliasPrefix = $"{sourceAlias}.";
        return columnName.StartsWith(aliasPrefix, StringComparison.OrdinalIgnoreCase)
            ? columnName[aliasPrefix.Length..]
            : columnName;
    }

    private static string GetRootFieldName(string columnName)
    {
        var separatorIndex = columnName.IndexOf('.', StringComparison.Ordinal);
        var rootSegment = separatorIndex < 0 ? columnName : columnName[..separatorIndex];
        var indexerIndex = rootSegment.IndexOf('[', StringComparison.Ordinal);
        return indexerIndex < 0 ? rootSegment : rootSegment[..indexerIndex];
    }

    private static string GetUnqualifiedFieldName(string fieldName)
    {
        var separatorIndex = fieldName.LastIndexOf('.');
        return separatorIndex < 0 ? fieldName : fieldName[(separatorIndex + 1)..];
    }

    private sealed record NestedRootField(PlanningField Field, int PrefixLength);
}
