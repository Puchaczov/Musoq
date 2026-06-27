using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;
public partial class SubqueryToCteRewriteVisitor
{
    private static (Node? RemainingExpression, List<SubqueryInfo> Subqueries) ExtractPredicateSubqueries(Node expression)
    {
        switch (expression)
        {
            case InQueryNode inQuery:
                return (null, [SubqueryInfo.CreateIn(inQuery, false)]);

            case NotNode { Expression: InQueryNode inQuery }:
                return (null, [SubqueryInfo.CreateIn(inQuery, true)]);

            case ExistsQueryNode existsQuery:
                return (null, [SubqueryInfo.CreateExists(existsQuery, false)]);

            case NotNode { Expression: ExistsQueryNode existsQuery }:
                return (null, [SubqueryInfo.CreateExists(existsQuery, true)]);

            case AndNode and:
            {
                var (leftRemaining, leftSubs) = ExtractPredicateSubqueries(and.Left);
                var (rightRemaining, rightSubs) = ExtractPredicateSubqueries(and.Right);

                var allSubs = leftSubs;
                allSubs.AddRange(rightSubs);

                var remaining = (leftRemaining, rightRemaining) switch
                {
                    (null, null) => null,
                    (null, _) => rightRemaining,
                    (_, null) => leftRemaining,
                    _ => new AndNode(leftRemaining, rightRemaining)
                };

                return (remaining, allSubs);
            }

            case OrNode:
            {
                var orSubqueries = new List<SubqueryInfo>();
                CollectPredicateSubqueries(expression, orSubqueries);

                if (orSubqueries.Count == 0)
                    return (expression, []);

                var markedSubs = orSubqueries
                    .Select(s => s with { RequiresLeftJoin = true })
                    .ToList();

                return (expression, markedSubs);
            }

            default:
                return (expression, []);
        }
    }
}
