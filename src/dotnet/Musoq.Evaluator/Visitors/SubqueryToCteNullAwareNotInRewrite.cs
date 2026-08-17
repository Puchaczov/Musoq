using System.Collections.Generic;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private bool TryRewriteNullAwareNotIn(
        SubqueryInfo subqueryInfo,
        PredicateSubqueryJoin match,
        ref FromNode currentFrom,
        ref Node? remainingExpr,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        if (subqueryInfo.RequiresLeftJoin || !subqueryInfo.IsIn || !subqueryInfo.IsNegated ||
            !TryCreateNullAwareNotInGuard(subqueryInfo, out var nullGuard))
            return false;

        currentFrom = new Parser.JoinFromNode(
            currentFrom,
            match.CteRef,
            match.JoinExpression,
            JoinType.LeftAntiSemi);
        var nullJoin = PreparePredicateSubqueryJoin(nullGuard, cteInnerExpressions);
        currentFrom = new Parser.JoinFromNode(
            currentFrom,
            nullJoin.CteRef,
            nullJoin.JoinExpression,
            JoinType.LeftAntiSemi);
        remainingExpr = AddNotInOuterNullGuard(remainingExpr, subqueryInfo.InQueryNode!.Left);
        return true;
    }

    private static Node? AddNotInOuterNullGuard(Node? expression, Node left)
    {
        var guard = new IsNullNode(left, true);
        return expression == null ? guard : new AndNode(expression, guard);
    }

    private static bool TryCreateNullAwareNotInGuard(
        SubqueryInfo subqueryInfo,
        out SubqueryInfo nullGuard)
    {
        if (subqueryInfo.InQueryNode is not { Subquery: QueryNode query } ||
            query.Select.Fields.Length != 1 ||
            query.GroupBy != null ||
            query.Window != null ||
            query.Qualify != null)
        {
            nullGuard = null!;
            return false;
        }

        var predicate = new IsNullNode(query.Select.Fields[0].Expression, false);
        var guardedQuery = AddSubqueryWherePredicate(query, predicate);
        nullGuard = SubqueryInfo.CreateExists(new ExistsQueryNode(guardedQuery), true) with
        {
            Correlation = subqueryInfo.Correlation
        };
        return true;
    }

    private bool TryCreateNullAwareNotInMatch(
        InQueryNode inQuery,
        SubqueryCorrelationAnalysis analysis,
        out SubqueryInfo matchInfo,
        out SubqueryInfo nullInfo)
    {
        if (inQuery.Subquery is not QueryNode query ||
            query.Select.Fields.Length != 1 ||
            query.GroupBy != null ||
            query.Window != null ||
            query.Qualify != null)
        {
            matchInfo = null!;
            nullInfo = null!;
            return false;
        }

        var value = query.Select.Fields[0].Expression;
        var matchQuery = AddSubqueryWherePredicate(query, new EqualityNode(value, inQuery.Left));
        var nullQuery = AddSubqueryWherePredicate(query, new IsNullNode(value, false));
        var correlation = FindCorrelation(inQuery, analysis);

        matchInfo = CreateNullAwareNotInExists(matchQuery, correlation);
        nullInfo = CreateNullAwareNotInExists(nullQuery, correlation);
        return true;
    }

    private static SubqueryInfo CreateNullAwareNotInExists(
        QueryNode query,
        SubqueryCorrelationInfo? correlation)
    {
        return SubqueryInfo.CreateExists(new ExistsQueryNode(query), true) with
        {
            RequiresLeftJoin = true,
            Correlation = correlation
        };
    }

    private static QueryNode AddSubqueryWherePredicate(QueryNode query, Node predicate)
    {
        var where = query.Where == null
            ? new WhereNode(predicate)
            : new WhereNode(new AndNode(query.Where.Expression, predicate));

        return new QueryNode(
            query.Select,
            query.From,
            where,
            query.GroupBy,
            query.OrderBy,
            query.Skip,
            query.Take,
            query.Window,
            query.Qualify,
            default);
    }

    private sealed partial class PredicateSubqueryExpressionRewriter
    {
        private Node PrepareNotInSubquery(InQueryNode inQuery)
        {
            var original = SubqueryInfo.CreateIn(inQuery, true);
            if (!owner.TryCreateNullAwareNotInMatch(inQuery, analysis, out var matchInfo, out var nullInfo))
                return PrepareSubquery(original).Replacement;

            var match = owner.PreparePredicateSubqueryJoin(matchInfo, cteInnerExpressions);
            _joins.Add(match);

            var nullMatch = owner.PreparePredicateSubqueryJoin(nullInfo, cteInnerExpressions);
            _joins.Add(nullMatch);

            return new AndNode(
                new IsNullNode(inQuery.Left, true),
                new AndNode(match.Replacement, nullMatch.Replacement));
        }
    }
}
