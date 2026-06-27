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
    private readonly Dictionary<string, List<string>> _columns = new();
    private readonly Dictionary<string, string> _aliasToColumnKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _generatedAliases = [];
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

            _cachedColumns = _columns.ToDictionary(f => f.Key, f => f.Value.Distinct().ToArray());
            _columnsCacheValid = true;
            return _cachedColumns;
        }
    }

    public override void Visit(AccessColumnNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _columns[ResolveColumnKey(node.Alias)].Add(node.Name);
    }

    public override void Visit(IdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _columns[_queryAlias].Add(node.Name);
    }

    public override void Visit(SchemaFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var alias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _queryAlias = alias + _schemaFromKey;

        if (_columns.ContainsKey(_queryAlias))
            throw new AliasAlreadyUsedException(node, _queryAlias);

        _generatedAliases.Add(_queryAlias);
        _columns.Add(_queryAlias, []);
        _aliasToColumnKey[alias] = _queryAlias;
    }

    public override void Visit(AliasedFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var alias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _queryAlias = alias + _schemaFromKey;
        _generatedAliases.Add(_queryAlias);
        _columns.Add(_queryAlias, []);
        _aliasToColumnKey[alias] = _queryAlias;
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
        _aliasToColumnKey[alias] = _queryAlias;
    }

    private string ResolveColumnKey(string? alias)
    {
        if (!string.IsNullOrEmpty(alias) && _aliasToColumnKey.TryGetValue(alias, out var columnKey))
            return columnKey;

        return _queryAlias;
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
    }

    public void QueryEnds()
    {
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
