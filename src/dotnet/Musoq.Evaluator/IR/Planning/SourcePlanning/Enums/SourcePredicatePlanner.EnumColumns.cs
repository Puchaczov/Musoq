using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourcePredicatePlanner
{
    private static ExpressionConverter CreateExpressionConverter(
        IReadOnlyDictionary<string, ISchemaColumn[]> inferredColumns)
    {
        return new ExpressionConverter(
            columnEnumTypeResolver: (alias, columnName) =>
                ResolveEnumType(inferredColumns, alias, columnName));
    }

    private static EnumTypeDescriptor? ResolveEnumType(
        IReadOnlyDictionary<string, ISchemaColumn[]> inferredColumns,
        string alias,
        string columnName)
    {
        if (!string.IsNullOrWhiteSpace(alias) && inferredColumns.TryGetValue(alias, out var aliased))
            return FindEnumType(aliased, columnName);

        var matches = inferredColumns.Values
            .SelectMany(static columns => columns)
            .Where(column => string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return matches.Length == 1 ? matches[0].EnumType : null;
    }

    private static EnumTypeDescriptor? FindEnumType(
        IEnumerable<ISchemaColumn> columns,
        string columnName)
    {
        return columns.FirstOrDefault(column =>
            string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))?.EnumType;
    }
}
