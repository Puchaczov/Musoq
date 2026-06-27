using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;
public partial class SubqueryToCteRewriteVisitor
{
    private static void CollectPredicateSubqueries(Node node, List<SubqueryInfo> result)
    {
        switch (node)
        {
            case InQueryNode inQuery:
                result.Add(SubqueryInfo.CreateIn(inQuery, false));
                break;
            case NotNode { Expression: InQueryNode inQuery }:
                result.Add(SubqueryInfo.CreateIn(inQuery, true));
                break;
            case ExistsQueryNode existsQuery:
                result.Add(SubqueryInfo.CreateExists(existsQuery, false));
                break;
            case NotNode { Expression: ExistsQueryNode existsQuery }:
                result.Add(SubqueryInfo.CreateExists(existsQuery, true));
                break;
            case AndNode and:
                CollectPredicateSubqueries(and.Left, result);
                CollectPredicateSubqueries(and.Right, result);
                break;
            case OrNode or:
                CollectPredicateSubqueries(or.Left, result);
                CollectPredicateSubqueries(or.Right, result);
                break;
            case NotNode not:
                CollectPredicateSubqueries(not.Expression, result);
                break;
        }
    }

    private static Node ReplacePredicateSubqueryNode(Node expression, SubqueryInfo target, Node replacement)
    {
        if (!target.IsNegated && ReferenceEquals(expression, target.PredicateNode))
            return replacement;

        if (target.IsNegated &&
            expression is NotNode { Expression: var nested } &&
            ReferenceEquals(nested, target.PredicateNode))
            return replacement;

        return expression switch
        {
            AndNode and => new AndNode(
                ReplacePredicateSubqueryNode(and.Left, target, replacement),
                ReplacePredicateSubqueryNode(and.Right, target, replacement)),
            OrNode or => new OrNode(
                ReplacePredicateSubqueryNode(or.Left, target, replacement),
                ReplacePredicateSubqueryNode(or.Right, target, replacement)),
            NotNode not => new NotNode(ReplacePredicateSubqueryNode(not.Expression, target, replacement)),
            _ => expression
        };
    }
}
