using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private int ResolvePropertyColumnIndex(PropertyFromNode node)
    {
        var sourceColumns = ResolvePropertyColumns(node, node.SourceAlias);
        if (sourceColumns == null)
            return -1;

        var propertyName = node.PropertiesChain[0].PropertyName;

        foreach (var column in sourceColumns)
        {
            if (string.Equals(column.ColumnName, propertyName, StringComparison.OrdinalIgnoreCase))
                return column.ColumnIndex;
        }

        return -1;
    }

    private OutputSchema BuildOutputSchema(params string?[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
                continue;

            if (!_inferredColumns.TryGetValue(alias, out var columns))
                continue;

            return CreateOutputSchema(columns);
        }

        return OutputSchema.Empty;
    }

    private OutputSchema BuildPropertyOutputSchema(PropertyFromNode node)
    {
        var columns = ResolvePropertyColumns(node, node.Alias);
        return columns == null ? OutputSchema.Empty : CreateOutputSchema(columns);
    }

    private IReadOnlyList<ISchemaColumn>? ResolvePropertyColumns(PropertyFromNode node, string alias)
    {
        if (
            node is Parser.PropertyFromNode { DeclarationScope: { } scope }
            && scope.ScopeSymbolTable.TryGetSymbol(alias, out Musoq.Evaluator.Utils.Symbols.TableSymbol? symbol)
            && symbol != null
            && symbol.ContainsAlias(alias)
        )
        {
            return symbol.GetTableByAlias(alias).Table.Columns;
        }

        return _inferredColumns.TryGetValue(alias, out var columns) ? columns : null;
    }

    private static OutputSchema CreateOutputSchema(IReadOnlyList<ISchemaColumn> columns)
    {
        var schemaColumns = new ColumnSchema[columns.Count];
        for (var i = 0; i < columns.Count; i++)
            schemaColumns[i] = new ColumnSchema(
                columns[i].ColumnName,
                columns[i].ColumnType,
                columns[i].ColumnIndex,
                columns[i].IntendedTypeName,
                columns[i].SourceReadType,
                columns[i].EnumType
            )
            {
                Stability = columns[i].Stability,
            };

        return new OutputSchema(schemaColumns);
    }
}
