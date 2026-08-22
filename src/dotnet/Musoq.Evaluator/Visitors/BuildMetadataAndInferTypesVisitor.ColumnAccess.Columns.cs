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
        PushSemanticNode(new AccessRawIdentifierNode(node.Name, node.ReturnType));
    }

    public override void Visit(AccessColumnNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (TryBindSetResultModifierColumn(node.Name, node.Alias, node))
            return;

        var hasProcessedQueryId = _sourceBinding.CurrentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId);
        var primaryIdentifier = hasProcessedQueryId
            ? _sourceBinding.CurrentScope[MetaAttributes.ProcessedQueryId]
            : _sourceBinding.Identifier;
        var identifier = string.IsNullOrEmpty(primaryIdentifier) ? node.Alias : primaryIdentifier;

        if (DiagnosticContext?.HasErrors == true &&
            _diagnosticRecoveryAliases.Contains(string.IsNullOrEmpty(node.Alias) ? identifier : node.Alias))
        {
            PushSemanticNode(new AccessColumnNode(node.Name, node.Alias, typeof(object), node.Span));
            return;
        }

        if (string.IsNullOrEmpty(identifier))
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                VisitorOperationNames.VisitAccessColumnNode,
                "No valid identifier found for column access",
                "Ensure the query has proper FROM clause and table aliases are correctly specified."
            );

        var tableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.TryGetSymbol<TableSymbol>(identifier, out var resolvedTableSymbol)
            ? resolvedTableSymbol
            : null;
        if (tableSymbol == null)
        {
            var missingAlias = !string.IsNullOrEmpty(node.Alias) ? node.Alias : identifier;
            if (DiagnosticContext?.HasErrors == true)
            {
                PushSemanticNode(new AccessColumnNode(node.Name, node.Alias, typeof(string), node.Span));
                return;
            }

            if (TryReportUnknownAlias(missingAlias, [], node))
            {
                PushSemanticNode(new AccessColumnNode(node.Name, node.Alias, typeof(string), node.Span));
                return;
            }
        }

        if (tableSymbol is null)
            throw new UnknownAliasException(identifier, node.SpanOrEmpty());

        if (!string.IsNullOrEmpty(node.Alias) && !tableSymbol.ContainsAlias(node.Alias))
        {
            if (TryReportUnknownAlias(node.Alias, tableSymbol.CompoundTables, node))
            {
                PushSemanticNode(new AccessColumnNode(node.Name, node.Alias, typeof(object), node.Span));
                return;
            }

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
        catch (Exception exception)
        {
            throw new SchemaProviderFailureException(exception);
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
        PushSemanticNode(accessColumn);
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
                inferredReplaceExpressions[i] = PopSemanticNode("Visit(AllColumnsNode).ReplaceItem");
        }

        if (!string.IsNullOrWhiteSpace(node.Alias) ||
            (!tableSymbol.IsCompoundTable && string.IsNullOrWhiteSpace(node.Alias)))
            ProcessSingleTable(node, tableSymbol, ResolveSingleTableStarIdentifier(node, tableSymbol, identifier), inferredReplaceExpressions);
        else if (tableSymbol.IsCompoundTable) ProcessCompoundTable(node, tableSymbol, inferredReplaceExpressions);

        PushSemanticNode(node);
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
        if (TryBindSetResultModifierColumn(node.Name, string.Empty, node))
            return;

        if (_queryState.QueryPart != QueryPart.From)
        {
            if (_queryState.QueryPart == QueryPart.OrderBy && _resultShape.SelectFieldAliases.TryGetValue(node.Name, out var aliasExpression))
            {
                PushSemanticNode(aliasExpression);
                return;
            }

            var tableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_sourceBinding.Identifier);
            var binding = _columnPropertyBindingService.ResolveIdentifier(tableSymbol, node.Name);
            if (binding.Kind == SemanticIdentifierBindingKind.Column)
            {
                var column = binding.Column ?? throw VisitorException.CreateForProcessingFailure(
                    VisitorName,
                    VisitorOperationNames.VisitAccessColumnNode,
                    $"Column binding for '{node.Name}' did not include a column.");
                Visit(new AccessColumnNode(column.ColumnName, binding.SourceAlias ?? string.Empty, column.ColumnType,
                    TextSpan.Empty, column.IntendedTypeName));
                return;
            }

            if (binding.Kind == SemanticIdentifierBindingKind.Identifier)
            {
                PushSemanticNode(new IdentifierNode(node.Name));
                return;
            }

            if (binding.Kind == SemanticIdentifierBindingKind.UnknownAlias &&
                TryReportUnknownAlias(binding.UnknownAlias!, binding.AvailableAliases, node))
            {
                PushSemanticNode(new IdentifierNode(node.Name));
                return;
            }

            if (TryReportOrThrowUnknownColumn(node.Name, binding.AvailableColumns, node))
                return;

            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                VisitorOperationNames.VisitAccessColumnNode,
                $"Column '{node.Name}' could not be resolved.",
                "Verify that the column exists in the current query scope.");
        }

        PushSemanticNode(new IdentifierNode(node.Name));
    }
}
