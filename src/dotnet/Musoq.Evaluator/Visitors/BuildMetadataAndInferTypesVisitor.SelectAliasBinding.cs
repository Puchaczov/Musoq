using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly Stack<Dictionary<string, FieldNode>> _selectAliasScopes = new();
    private readonly HashSet<string> _activeSelectAliasReferences = new(StringComparer.OrdinalIgnoreCase);

    internal void PrecollectCurrentQuerySelectAliases(SelectNode select)
    {
        ArgumentNullException.ThrowIfNull(select);

        var aliases = new Dictionary<string, FieldNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in select.Fields)
        {
            if (field.Expression is AllColumnsNode)
                continue;

            var alias = field.FieldName;
            if (string.IsNullOrWhiteSpace(alias))
                continue;

            if (string.Equals(alias, field.Expression.ToString(), StringComparison.Ordinal))
                continue;

            aliases.TryAdd(alias, field);
        }

        _selectAliasScopes.Push(aliases);
    }

    internal void EndCurrentQuerySelectAliasScope()
    {
        if (_selectAliasScopes.Count > 0)
            _selectAliasScopes.Pop();
    }

    internal bool TryGetSelectAliasExpressionForCurrentClause(string identifier, out Node expression)
    {
        expression = null!;

        if (!CanCurrentClauseUseSelectAlias())
            return false;

        if (_selectAliasScopes.Count == 0)
            return false;

        if (_activeSelectAliasReferences.Contains(identifier))
            return false;

        if (IsCurrentSourceColumnForSelectAlias(identifier))
            return false;

        if (!_selectAliasScopes.Peek().TryGetValue(identifier, out var field))
            return false;

        expression = CloneExpression(field.Expression);
        return true;
    }

    internal void EnterSelectAliasReference(string identifier)
    {
        _activeSelectAliasReferences.Add(identifier);
    }

    internal void ExitSelectAliasReference(string identifier)
    {
        _activeSelectAliasReferences.Remove(identifier);
    }

    private bool CanCurrentClauseUseSelectAlias()
    {
        return _queryState.QueryPart is QueryPart.Where or QueryPart.GroupBy or QueryPart.Having;
    }

    private bool IsCurrentSourceColumnForSelectAlias(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var hasProcessedQueryId = _sourceBinding.CurrentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId);
        var identifier = hasProcessedQueryId
            ? _sourceBinding.CurrentScope[MetaAttributes.ProcessedQueryId]
            : _sourceBinding.Identifier;

        if (string.IsNullOrEmpty(identifier))
            return false;

        if (!_sourceBinding.CurrentScope.ScopeSymbolTable.TryGetSymbol<TableSymbol>(identifier, out var tableSymbol))
            return false;

        try
        {
            if (tableSymbol.GetColumnByAliasAndName(identifier, name) != null)
                return true;
        }
        catch (KeyNotFoundException)
        {
        }
        catch (InvalidOperationException)
        {
        }

        try
        {
            var (_, table, _) = tableSymbol.GetTableByColumnName(name);
            return table != null;
        }
        catch (AmbiguousColumnException)
        {
            return true;
        }
    }

    private static Node CloneExpression(Node expression)
    {
        var cloneVisitor = new SelectAliasCloneVisitor();
        var cloneTraverser = new CloneTraverseVisitor(cloneVisitor);
        expression.Accept(cloneTraverser);
        return cloneVisitor.ClonedNode;
    }

    private sealed class SelectAliasCloneVisitor : CloneQueryVisitor
    {
        public Node ClonedNode => Nodes.Peek();
    }
}
