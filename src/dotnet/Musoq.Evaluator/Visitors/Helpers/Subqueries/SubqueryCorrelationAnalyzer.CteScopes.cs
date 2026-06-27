using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed partial class SubqueryCorrelationAnalyzer
{
    public override void Visit(CteExpressionNode node)
    {
        var forbiddenAliases = CreateAliasSet(GetVisibleAliases().Concat(CollectAliasesFromNode(node.OuterExpression)));
        var savedScopes = ClearQueryScopes();

        _forbiddenAliasScopes.Push(forbiddenAliases);
        try
        {
            foreach (var exp in node.InnerExpression)
                exp.Accept(this);
        }
        finally
        {
            _forbiddenAliasScopes.Pop();
            RestoreQueryScopes(savedScopes);
        }

        node.OuterExpression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        _cteDefinitionDepth += 1;
        try
        {
            node.Value.Accept(this);
        }
        finally
        {
            _cteDefinitionDepth -= 1;
        }

        node.Accept(Visitor);
    }
}
