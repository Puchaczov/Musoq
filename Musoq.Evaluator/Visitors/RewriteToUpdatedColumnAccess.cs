using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using PropertyFromNode = Musoq.Parser.Nodes.From.PropertyFromNode;

namespace Musoq.Evaluator.Visitors;

public class RewriteToUpdatedColumnAccess(IReadOnlyDictionary<string, string> usedTables) : CloneQueryVisitor
{
    private FromNode _from;
    private WhereNode _where;

    public WhereNode Where
    {
        get
        {
            if (_where is not null) return _where;

            _where = (WhereNode)Nodes.Pop();

            return _where;
        }
    }

    public FromNode From
    {
        get
        {
            if (_from is not null) return _from;

            _from = (FromNode)Nodes.Pop();

            return _from;
        }
    }

    public override void Visit(AccessColumnNode node)
    {
        base.Visit(usedTables.TryGetValue(node.Alias, out var alias)
            ? new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), alias, node.ReturnType,
                TextSpan.Empty, node.IntendedTypeName)
            : node);
    }

    public override void Visit(PropertyFromNode node)
    {
        base.Visit(usedTables.TryGetValue(node.SourceAlias, out var alias)
            ? new Parser.PropertyFromNode(node.Alias, alias, node.PropertiesChain.Select((p, i) =>
            {
                if (i == 0)
                    return p with
                    {
                        PropertyName = NamingHelper.ToColumnName(node.SourceAlias, p.PropertyName)
                    };

                return p;
            }).ToArray())
            : node);
    }
}
