using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;
public partial class SubqueryToCteRewriteVisitor
{
    private (QueryNode Rewritten, List<CteInnerExpressionNode> InnerCtes) RewriteNestedInSubqueries(QueryNode subquery)
    {
        if (subquery.Where == null)
            return (subquery, []);

        var (remainingExpr, nestedSubqueries) = AttachCorrelation(
            ExtractPredicateSubqueries(subquery.Where.Expression),
            AnalyzeSubqueries(subquery));

        if (nestedSubqueries.Count == 0)
            return (subquery, []);

        var innerCtes = new List<CteInnerExpressionNode>();
        var currentFrom = UnwrapFromForJoin(subquery.From);

        (currentFrom, remainingExpr) = RewriteSubqueriesToJoins(
            nestedSubqueries, currentFrom, remainingExpr, innerCtes);

        var joinNode = new Parser.JoinNode((Parser.JoinFromNode)currentFrom);
        var wrappedFrom = new Parser.ExpressionFromNode(joinNode);
        var newWhere = remainingExpr != null ? new WhereNode(remainingExpr) : null;

        var rewrittenQuery = new QueryNode(
            subquery.Select,
            wrappedFrom,
            newWhere,
            subquery.GroupBy,
            subquery.OrderBy,
            subquery.Skip,
            subquery.Take,
            subquery.Window,
            subquery.Qualify,
            default);

        return (rewrittenQuery, innerCtes);
    }
}
