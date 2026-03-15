using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public class ExtractRawColumnsVisitor : NoOpExpressionVisitor, IAwareExpressionVisitor
{
    private readonly Dictionary<string, List<string>> _columns = new();
    private readonly List<string> _generatedAliases = [];
    private IReadOnlyDictionary<string, string[]> _cachedColumns;
    private bool _columnsCacheValid;
    private string _queryAlias;
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
        _columns[_queryAlias].Add(node.Name);
    }

    public override void Visit(IdentifierNode node)
    {
        _columns[_queryAlias].Add(node.Name);
    }

    public override void Visit(SchemaFromNode node)
    {
        _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString()) +
                      _schemaFromKey;

        if (_columns.ContainsKey(_queryAlias))
            throw new AliasAlreadyUsedException(node, _queryAlias);

        _generatedAliases.Add(_queryAlias);
        _columns.Add(_queryAlias, []);
    }

    public override void Visit(AliasedFromNode node)
    {
        _queryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _generatedAliases, _schemaFromKey.ToString()) +
                      _schemaFromKey;
        _generatedAliases.Add(_queryAlias);
        _columns.Add(_queryAlias, []);
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

    public void SetTheMostInnerIdentifierOfDotNode(IdentifierNode node)
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
