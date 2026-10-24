using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Logical;

internal sealed class LogicalColumnMetadata(IReadOnlyDictionary<string, ISchemaColumn[]> inferredColumns)
{
    internal ColumnStability ResolveColumnStability(string alias, string columnName)
    {
        if (!string.IsNullOrWhiteSpace(alias) && inferredColumns.TryGetValue(alias, out var aliasedColumns))
            return FindColumnStability(aliasedColumns, columnName);

        var matches = inferredColumns.Values
            .SelectMany(static columns => columns)
            .Where(column => string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return matches.Length == 1 ? matches[0].Stability : ColumnStability.Stable;
    }

    private static ColumnStability FindColumnStability(
        IEnumerable<ISchemaColumn> columns,
        string columnName)
    {
        return columns.FirstOrDefault(column =>
                   string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))?.Stability
               ?? ColumnStability.Stable;
    }

    internal EnumTypeDescriptor? ResolveColumnEnumType(string alias, string columnName)
    {
        if (!string.IsNullOrWhiteSpace(alias) && inferredColumns.TryGetValue(alias, out var aliasedColumns))
            return FindColumnEnumType(aliasedColumns, columnName);

        var matches = inferredColumns.Values
            .SelectMany(static columns => columns)
            .Where(column => string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return matches.Length == 1 ? matches[0].EnumType : null;
    }

    private static EnumTypeDescriptor? FindColumnEnumType(
        IEnumerable<ISchemaColumn> columns,
        string columnName)
    {
        return columns.FirstOrDefault(column =>
            string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))?.EnumType;
    }

}
