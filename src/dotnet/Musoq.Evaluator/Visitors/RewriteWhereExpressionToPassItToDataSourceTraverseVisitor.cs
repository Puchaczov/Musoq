using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class RewriteWhereExpressionToPassItToDataSourceTraverseVisitor(IExpressionVisitor visitor)
    : CloneTraverseVisitor(visitor)
{
    public override void Visit(NotNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (Visitor is not RewriteWhereExpressionToPassItToDataSourceVisitor rewriter)
        {
            base.Visit(node);
            return;
        }

        var substitutionCount = rewriter.PredicateSubstitutionCount;
        node.Expression.Accept(this);
        rewriter.CompleteNegation(node, substitutionCount);
    }
}
