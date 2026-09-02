using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class ExtractRawColumnsVisitor : NoOpExpressionVisitor, IAwareExpressionVisitor
{
    private sealed class QueryScope
    {
        public string QueryAlias { get; set; } = string.Empty;

        public Dictionary<string, string> AliasToColumnKey { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<string> SourceKeys { get; } = [];
    }

    private readonly Dictionary<string, List<string>> _columns = new();
    private readonly List<string> _generatedAliases = [];
    private readonly HashSet<string> _completeSchemaColumnKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<QueryScope> _queryScopes = new();
    private IReadOnlyDictionary<string, string[]> _cachedColumns = new Dictionary<string, string[]>();
    private bool _columnsCacheValid;
    private string _queryAlias = string.Empty;
    private int _schemaFromKey;

    public IReadOnlyDictionary<string, string[]> Columns
    {
        get
        {
            if (_columnsCacheValid)
                return _cachedColumns;

            _cachedColumns = _columns.ToDictionary(
                f => f.Key,
                f => _completeSchemaColumnKeys.Contains(f.Key)
                    ? []
                    : f.Value.Distinct().ToArray());
            _columnsCacheValid = true;
            return _cachedColumns;
        }
    }

    public override void Visit(AccessColumnNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddColumn(ResolveColumnKey(node.Alias), node.Name);
    }

    public override void Visit(IdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddColumn(_queryAlias, node.Name);
    }

    public override void Visit(SchemaFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var alias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _queryAlias = alias + _schemaFromKey;

        if (_columns.ContainsKey(_queryAlias))
            throw new AliasAlreadyUsedException(
                node,
                string.IsNullOrWhiteSpace(node.Alias) ? _queryAlias : node.Alias);

        _generatedAliases.Add(_queryAlias);
        _columns.Add(_queryAlias, []);
        RegisterSource(alias, _queryAlias);
    }

    public override void Visit(AliasedFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var alias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _queryAlias = alias + _schemaFromKey;
        _generatedAliases.Add(_queryAlias);
        _columns.Add(_queryAlias, []);
        RegisterSource(alias, _queryAlias);
    }

    public override void Visit(ValuesFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var alias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _queryAlias = alias + _schemaFromKey;

        if (_columns.ContainsKey(_queryAlias))
            throw new AliasAlreadyUsedException(_queryAlias, node.HasSpan ? node.Span : TextSpan.Empty);

        _generatedAliases.Add(_queryAlias);
        _columns.Add(_queryAlias, []);
        RegisterSource(alias, _queryAlias);
    }

    private string ResolveColumnKey(string? alias)
    {
        if (!string.IsNullOrEmpty(alias) && TryResolveColumnKey(alias, out var columnKey))
            return columnKey;

        return _queryAlias;
    }

    private bool TryResolveColumnKey(string alias, out string columnKey)
    {
        foreach (var scope in _queryScopes)
            if (scope.AliasToColumnKey.TryGetValue(alias, out var resolvedColumnKey))
            {
                columnKey = resolvedColumnKey;
                return true;
            }

        columnKey = string.Empty;
        return false;
    }

    private void RegisterSource(string alias, string columnKey)
    {
        if (_queryScopes.Count == 0)
            return;

        var scope = _queryScopes.Peek();
        scope.AliasToColumnKey[alias] = columnKey;
        scope.SourceKeys.Add(columnKey);
        scope.QueryAlias = columnKey;
        InvalidateColumnsCache();
    }

    private void AddColumn(string columnKey, string columnName)
    {
        if (!_columns.TryGetValue(columnKey, out var columns))
        {
            columns = [];
            _columns[columnKey] = columns;
        }

        columns.Add(columnName);
        InvalidateColumnsCache();
    }

    private void InvalidateColumnsCache()
    {
        _columnsCacheValid = false;
    }

    internal void MarkProjectionWildcard(AllColumnsNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_queryScopes.Count == 0)
            return;

        var scope = _queryScopes.Peek();
        if (string.IsNullOrEmpty(node.Alias))
        {
            foreach (var sourceKey in scope.SourceKeys)
                _completeSchemaColumnKeys.Add(sourceKey);
        }
        else if (TryResolveColumnKey(node.Alias, out var columnKey))
        {
            _completeSchemaColumnKeys.Add(columnKey);
        }

        InvalidateColumnsCache();
    }

    public void SetScope(Scope scope)
    {
    }

    public void SetQueryPart(QueryPart part)
    {
    }

    public void QueryBegins()
    {
        _schemaFromKey += 1;
        _queryScopes.Push(new QueryScope());
        _queryAlias = string.Empty;
    }

    public void QueryEnds()
    {
        if (_queryScopes.Count == 0)
            return;

        _queryScopes.Pop();
        _queryAlias = _queryScopes.Count == 0
            ? string.Empty
            : _queryScopes.Peek().QueryAlias;
    }

    public void SetTheMostInnerIdentifierOfDotNode(IdentifierNode? node)
    {
    }

    public void InnerCteBegins()
    {
    }

    public void InnerCteEnds()
    {
    }

    public bool IsCurrentContextColumn(string name)
    {
        return false;
    }

    public void SetOperatorLeftFinished()
    {
    }
}
