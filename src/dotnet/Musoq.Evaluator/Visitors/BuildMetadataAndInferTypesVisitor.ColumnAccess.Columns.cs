using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(AccessRawIdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
    }

    public override void Visit(AccessColumnNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        try
        {
            var hasProcessedQueryId = _sourceBinding.CurrentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId);
            var primaryIdentifier = hasProcessedQueryId
                ? _sourceBinding.CurrentScope[MetaAttributes.ProcessedQueryId]
                : _sourceBinding.Identifier;
            var identifier = string.IsNullOrEmpty(primaryIdentifier) ? node.Alias : primaryIdentifier;

            if (string.IsNullOrEmpty(identifier))
                throw VisitorException.CreateForProcessingFailure(
                    VisitorName,
                    VisitorOperationNames.VisitAccessColumnNode,
                    "No valid identifier found for column access",
                    "Ensure the query has proper FROM clause and table aliases are correctly specified."
                );

            var tableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);
            if (tableSymbol == null)
                throw VisitorException.CreateForProcessingFailure(
                    VisitorName,
                    VisitorOperationNames.VisitAccessColumnNode,
                    $"Table symbol not found for identifier '{identifier}'",
                    "Verify that the table or alias is properly defined in the query."
                );

            if (!string.IsNullOrEmpty(node.Alias) && !tableSymbol.ContainsAlias(node.Alias))
            {
                if (TryReportUnknownAlias(node.Alias, tableSymbol.CompoundTables, node))
                    return;

                throw VisitorException.CreateForProcessingFailure(
                    VisitorName,
                    VisitorOperationNames.VisitAccessColumnNode,
                    $"Unknown alias '{node.Alias}'",
                    "Verify that the alias is defined in the FROM or JOIN clause.");
            }

            var tuple = !string.IsNullOrEmpty(node.Alias)
                ? tableSymbol.GetTableByAlias(node.Alias)
                : tableSymbol.GetTableByColumnName(node.Name);

            ISchemaColumn? column;
            try
            {
                column = tuple.Table?.GetColumnByName(node.Name);
            }
            catch (KeyNotFoundException)
            {
                column = null;
            }
            catch (InvalidOperationException)
            {
                column = null;
            }

            if (column == null)
            {
                TryReportOrThrowUnknownColumn(node.Name, tuple.Table?.Columns ?? [], node);
                return;
            }

            if (tuple.TableName == null)
            {
                TryReportOrThrowUnknownColumn(node.Name, tuple.Table?.Columns ?? [], node);
                return;
            }

            AddAssembly(column.ColumnType.Assembly);
            node.ChangeReturnType(column.ColumnType);

            var usedColumns = _sourceBinding.UsedColumns
                .Where(c => c.Key.Alias == tuple.TableName && c.Key.QueryId == _sourceBinding.SchemaFromKey)
                .Select(f => f.Value)
                .FirstOrDefault();

            if (usedColumns is not null)
                if (usedColumns.All(c => c.ColumnName != column.ColumnName))
                    usedColumns.Add(column);

            var accessColumn = new AccessColumnNode(column.ColumnName, tuple.TableName, column.ColumnType, node.Span,
                column.IntendedTypeName);
            Nodes.Push(accessColumn);
        }
        catch (Exception ex) when (ex is not VisitorException)
        {
            throw new VisitorException(
                VisitorName,
                VisitorOperationNames.VisitAccessColumnNode,
                $"Failed to process column access for '{node.Name}': {ex.Message}. " +
                "Check that the column exists in the specified table and that table aliases are correct.",
                ex
            );
        }
    }

    public override void Visit(AllColumnsNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var identifier = _sourceBinding.Identifier;
        var tableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);

        Node[]? inferredReplaceExpressions = null;
        if (node.ReplaceItems is { Length: > 0 })
        {
            inferredReplaceExpressions = new Node[node.ReplaceItems.Length];
            for (var i = node.ReplaceItems.Length - 1; i >= 0; i--)
                inferredReplaceExpressions[i] = Nodes.Pop();
        }

        if (!string.IsNullOrWhiteSpace(node.Alias) ||
            (!tableSymbol.IsCompoundTable && string.IsNullOrWhiteSpace(node.Alias)))
            ProcessSingleTable(node, tableSymbol, ResolveSingleTableStarIdentifier(node, tableSymbol, identifier), inferredReplaceExpressions);
        else if (tableSymbol.IsCompoundTable) ProcessCompoundTable(node, tableSymbol, inferredReplaceExpressions);

        Nodes.Push(node);
    }

    private static string ResolveSingleTableStarIdentifier(
        AllColumnsNode node,
        TableSymbol tableSymbol,
        string identifier)
    {
        return string.IsNullOrWhiteSpace(node.Alias) && tableSymbol.CompoundTables.Length == 1
            ? tableSymbol.CompoundTables[0]
            : identifier;
    }

    public override void Visit(IdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_queryState.QueryPart != QueryPart.From)
        {
            if (_queryState.QueryPart == QueryPart.OrderBy && _resultShape.SelectFieldAliases.TryGetValue(node.Name, out var aliasExpression))
            {
                Nodes.Push(aliasExpression);
                return;
            }

            var tableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_sourceBinding.Identifier);
            var column = tableSymbol.GetColumnByAliasAndName(_sourceBinding.Identifier, node.Name);

            if (column == null)
            {
                if (tableSymbol.IsCompoundTable)
                {
                    var (_, table, sourceAlias) = tableSymbol.GetTableByColumnName(node.Name);
                    if (table != null && sourceAlias != null)
                    {
                        var columns = table.GetColumnsByName(node.Name);
                        var singleCol = columns[0];
                        Visit(new AccessColumnNode(singleCol.ColumnName, sourceAlias, singleCol.ColumnType,
                            TextSpan.Empty, singleCol.IntendedTypeName));
                        return;
                    }

                    if (node.Name != _sourceBinding.Identifier && TryResolveIdentifierAsSingleColumnAlias(tableSymbol, node.Name))
                        return;
                }

            if (node.Name == _sourceBinding.Identifier)
            {
                Nodes.Push(new IdentifierNode(node.Name));
                return;
            }

            if (TryReportOrThrowUnknownColumn(node.Name, tableSymbol.GetColumns(), node))
                return;

            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                VisitorOperationNames.VisitAccessColumnNode,
                $"Column '{node.Name}' could not be resolved.",
                "Verify that the column exists in the current query scope.");
        }

            Visit(new AccessColumnNode(node.Name, string.Empty, column.ColumnType, TextSpan.Empty,
                column.IntendedTypeName));
            return;
        }

        Nodes.Push(new IdentifierNode(node.Name));
    }

    private bool TryResolveIdentifierAsSingleColumnAlias(TableSymbol tableSymbol, string identifierName)
    {
        if (!tableSymbol.ContainsAlias(identifierName))
            return false;

        if (!tableSymbol.TryGetColumns(identifierName, out var aliasColumns))
            return false;

        if (aliasColumns is not { Length: 1 })
            return false;

        var onlyColumn = aliasColumns[0];
        Visit(new AccessColumnNode(onlyColumn.ColumnName, identifierName, onlyColumn.ColumnType,
            TextSpan.Empty, onlyColumn.IntendedTypeName));
        return true;
    }
}
