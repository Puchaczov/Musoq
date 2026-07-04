using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private void ProcessSingleTable(AllColumnsNode node, TableSymbol tableSymbol, string identifier, Node[]? inferredReplaceExpressions)
    {
        var generatedColumnIdentifier = node.Alias ?? identifier;
        (ISchema Schema, ISchemaTable Table, string TableName) tuple;
        try
        {
            tuple = tableSymbol.GetTableByAlias(generatedColumnIdentifier);
        }
        catch (KeyNotFoundException)
        {
            var span = node.SpanOrEmpty();
            throw new UnknownColumnOrAliasException(
                generatedColumnIdentifier,
                "in wildcard projection",
                span);
        }

        var table = tuple.Table;

        var eligibleColumns = new List<ISchemaColumn>();
        foreach (var column in table.Columns)
            if (BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(column.ColumnType))
                eligibleColumns.Add(column);

        var filteredColumns = node.HasModifiers
            ? ApplyStarModifiers(node, eligibleColumns, inferredReplaceExpressions)
            : null;

        var generatedColumns = GetOrCreateGeneratedColumns(generatedColumnIdentifier);

        if (filteredColumns != null)
        {
            var projectedColumns = CreateSingleTableStarProjectedColumns(
                tableSymbol,
                generatedColumnIdentifier,
                filteredColumns);
            ApplyRenameSubstitution(node, projectedColumns, node.SpanOrEmpty());

            var positionCounter = 0;
            foreach (var entry in projectedColumns)
            {
                if (entry.ReplacementExpression != null)
                {
                    AddAssembly((entry.ReplacementExpression.ReturnType ??
                                 throw new InvalidOperationException($"Replacement expression for '{entry.Column.ColumnName}' has no inferred return type.")).Assembly);
                    generatedColumns.Add(new FieldNode(entry.ReplacementExpression, positionCounter++, entry.OutputName,
                        false));
                }
                else
                {
                    AddColumnToGeneratedColumns(
                        tableSymbol,
                        entry.Column,
                        positionCounter++,
                        generatedColumnIdentifier,
                        generatedColumns,
                        false,
                        entry.OutputName);
                }
            }
        }
        else
        {
            var positionCounter = 0;
            foreach (var column in eligibleColumns)
                AddColumnToGeneratedColumns(tableSymbol, column, positionCounter++, generatedColumnIdentifier,
                    generatedColumns);
        }

        UpdateUsedColumns(generatedColumnIdentifier, table);
    }

    private void ProcessCompoundTable(AllColumnsNode node, TableSymbol tableSymbol, Node[]? inferredReplaceExpressions)
    {
        if (!node.HasModifiers)
        {
            foreach (var tableIdentifier in tableSymbol.CompoundTables)
            {
                var tuple = tableSymbol.GetTableByAlias(tableIdentifier);
                var table = tuple.Table;

                var generatedColumns = GetOrCreateGeneratedColumns(tableIdentifier);

                var positionCounter = 0;
                foreach (var column in table.Columns)
                    if (BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(column.ColumnType))
                        AddColumnToGeneratedColumns(tableSymbol, column, positionCounter++, tableIdentifier,
                            generatedColumns, true);

                UpdateUsedColumns(tableIdentifier, table);
            }

            return;
        }

        var allEligible = new List<(string TableIdentifier, ISchemaColumn Column)>();
        var tablesByIdentifier =
            new Dictionary<string, (ISchemaTable Table, TableSymbol Symbol)>(StringComparer.OrdinalIgnoreCase);

        foreach (var tableIdentifier in tableSymbol.CompoundTables)
        {
            var tuple = tableSymbol.GetTableByAlias(tableIdentifier);
            var table = tuple.Table;
            tablesByIdentifier[tableIdentifier] = (table, tableSymbol);

            foreach (var column in table.Columns)
                if (BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(column.ColumnType))
                    allEligible.Add((tableIdentifier, column));
        }

        var eligibleAsColumns = allEligible.Select(e => e.Column).ToList();
        var filtered = ApplyStarModifiers(node, eligibleAsColumns, inferredReplaceExpressions);
        var projected = CreateCompoundTableStarProjectedColumns(allEligible, filtered);
        ApplyRenameSubstitution(node, projected, node.SpanOrEmpty());

        foreach (var tableIdentifier in tableSymbol.CompoundTables)
        {
            var (table, _) = tablesByIdentifier[tableIdentifier];
            var generatedColumns = GetOrCreateGeneratedColumns(tableIdentifier);

            var positionCounter = 0;
            foreach (var column in table.Columns)
            {
                if (!BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(column.ColumnType))
                    continue;

                var projectedEntry = projected.FirstOrDefault(e =>
                    e.TableIdentifier == tableIdentifier && e.Column == column);
                if (projectedEntry == null)
                    continue;

                if (projectedEntry.ReplacementExpression != null)
                {
                    AddAssembly((projectedEntry.ReplacementExpression.ReturnType ?? typeof(object)).Assembly);
                    generatedColumns.Add(new FieldNode(projectedEntry.ReplacementExpression, positionCounter++,
                        projectedEntry.OutputName, false));
                }
                else
                {
                    AddColumnToGeneratedColumns(tableSymbol, column, positionCounter++, tableIdentifier,
                        generatedColumns, true, projectedEntry.OutputName);
                }
            }

            UpdateUsedColumns(tableIdentifier, table);
        }
    }

    private static int FindOriginalIndex(
        List<(string TableIdentifier, ISchemaColumn Column)> allEligible,
        ISchemaColumn column)
    {
        for (var i = 0; i < allEligible.Count; i++)
            if (allEligible[i].Column == column)
                return i;

        return -1;
    }
}
