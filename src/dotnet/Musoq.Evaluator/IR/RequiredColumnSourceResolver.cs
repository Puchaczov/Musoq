using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Planning;

internal static class RequiredColumnSourceResolver
{
    internal static (SourceReference Source, string ColumnName)[] Find(
        IReadOnlyList<SourceReference> sources,
        string columnName)
    {
        return sources
            .Select(source => (Source: source, ColumnName: Resolve(source, columnName)))
            .Where(static match => match.ColumnName != null)
            .Select(static match => (Source: match.Source, ColumnName: match.ColumnName!))
            .ToArray();
    }

    private static string? Resolve(SourceReference source, string columnName)
    {
        if (source.OutputColumns.Count == 0)
            return columnName;

        var exactColumn = source.OutputColumns.FirstOrDefault(output =>
            string.Equals(output, columnName, StringComparison.OrdinalIgnoreCase));
        if (exactColumn != null)
            return exactColumn;

        var rootColumnName = GetRootColumnName(columnName);
        return source.OutputColumns.FirstOrDefault(output =>
            string.Equals(output, rootColumnName, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRootColumnName(string columnName)
    {
        var separatorIndex = columnName.IndexOfAny(['.', '[']);
        return separatorIndex < 0 ? columnName : columnName[..separatorIndex];
    }
}
