using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class ExtractRawColumnsVisitor
{
    public override void Visit(UnpivotFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var alias = AliasGenerator.CreateAliasIfEmpty(
            node.Alias,
            _generatedAliases,
            _schemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _queryAlias = alias + _schemaFromKey;

        if (_columns.ContainsKey(_queryAlias))
            throw new AliasAlreadyUsedException(_queryAlias, node.HasSpan ? node.Span : TextSpan.Empty);

        _generatedAliases.Add(_queryAlias);
        _columns.Add(_queryAlias, []);
        _aliasToColumnKey[alias] = _queryAlias;
    }
}
