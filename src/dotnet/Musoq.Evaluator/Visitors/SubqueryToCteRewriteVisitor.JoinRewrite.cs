using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private (FromNode CurrentFrom, Node? RemainingExpr) RewriteSubqueriesToJoins(
        List<SubqueryInfo> subqueries,
        FromNode currentFrom,
        Node? remainingExpr,
        List<CteInnerExpressionNode> cteInnerExpressions
    )
    {
        foreach (var subqueryInfo in subqueries)
        {
            var join = PreparePredicateSubqueryJoin(subqueryInfo, cteInnerExpressions);
            if (subqueryInfo.RequiresLeftJoin)
            {
                currentFrom = new Parser.JoinFromNode(currentFrom, join.CteRef, join.JoinExpression, JoinType.LeftMark);
                if (remainingExpr != null)
                {
                    var value = PreparePredicateValue(
                        subqueryInfo,
                        join,
                        cteInnerExpressions,
                        witness =>
                            currentFrom = new Parser.JoinFromNode(
                                currentFrom,
                                witness.CteRef,
                                witness.JoinExpression,
                                JoinType.LeftMark
                            )
                    );
                    remainingExpr = ReplacePredicateSubqueryNode(remainingExpr, subqueryInfo, value);
                }
            }
            else if (subqueryInfo.IsNegated)
            {
                currentFrom = new Parser.JoinFromNode(
                    currentFrom,
                    join.CteRef,
                    join.JoinExpression,
                    JoinType.LeftAntiSemi
                );
                if (join.Membership is not null)
                    ApplyNotInNullGuards(join, ref currentFrom, ref remainingExpr, cteInnerExpressions);
            }
            else
            {
                currentFrom = new Parser.JoinFromNode(currentFrom, join.CteRef, join.JoinExpression, JoinType.LeftSemi);
            }
        }

        return (currentFrom, remainingExpr);
    }

    private PredicateSubqueryJoin PreparePredicateSubqueryJoin(
        SubqueryInfo subqueryInfo,
        List<CteInnerExpressionNode> cteInnerExpressions
    )
    {
        var cteName = CreateUniqueSubqueryName();
        var subquery = subqueryInfo.Subquery;
        var subqueryBody = UnwrapCteSubqueryBody(subquery, cteInnerExpressions);
        var rewrite = subqueryInfo.IsExists
            ? PrepareExistsSubquery(subqueryInfo, subqueryBody, cteName, cteInnerExpressions)
            : PrepareInSubquery(subqueryInfo, subqueryBody, cteName, cteInnerExpressions);

        cteInnerExpressions.Add(new CteInnerExpressionNode(rewrite.CteBody, cteName));

        var diagnosticSpan = rewrite.DiagnosticNode.SpanOrEmpty();
        var cteColumnRef = new AccessColumnNode(rewrite.CteColumnName, cteName, diagnosticSpan);
        EqualityNode match = subqueryInfo.IsIn
            ? new MaterializedInEqualityNode(rewrite.LeftSide, cteColumnRef)
            : new EqualityNode(rewrite.LeftSide, cteColumnRef);
        Node joinExpression = match.WithSpan(diagnosticSpan);
        if (rewrite.CorrelationJoinExpression != null)
        {
            if (
                subqueryInfo.RequiresLeftJoin
                && !IsCardinalitySafeApplyCorrelation(subqueryInfo, rewrite.CorrelationJoinExpression)
            )
            {
                ThrowUnsupportedPredicateApplyCorrelation(subqueryInfo);
            }

            joinExpression = new AndNode(joinExpression, rewrite.CorrelationJoinExpression);
        }

        var cteRef = new Parser.InMemoryTableFromNode(cteName, cteName);
        var replacement = new IsNullNode(
            new AccessColumnNode(rewrite.CteColumnName, cteName, default),
            !subqueryInfo.IsNegated
        );

        return new PredicateSubqueryJoin(cteRef, joinExpression, replacement, subqueryInfo.IsIn ? rewrite : null);
    }

    private SubqueryJoinRewrite PrepareInSubquery(
        SubqueryInfo subqueryInfo,
        Node subqueryBody,
        string cteName,
        List<CteInnerExpressionNode> cteInnerExpressions
    )
    {
        var inQueryNode =
            subqueryInfo.InQueryNode ?? throw new InvalidOperationException("IN subquery rewrite requires an IN node.");
        var leafQuery = GetLeftmostQuery(subqueryBody);

        if (leafQuery.Select.Fields.Length != 1)
            throw new VisitorException(
                VisitorName,
                "IN subquery validation",
                "Subquery used with IN must return exactly one column.",
                DiagnosticCode.MQ3049_InSubqueryMultipleColumns,
                inQueryNode.Span
            );

        if (leafQuery.Select.Fields[0].Expression is AllColumnsNode { HasModifiers: true })
            throw new VisitorException(
                VisitorName,
                "IN subquery validation",
                "Star modifiers (EXCLUDE, REPLACE, LIKE) are not supported inside IN subqueries. Use an explicit column reference instead.",
                DiagnosticCode.MQ3054_StarModifierInInSubquery,
                inQueryNode.Span
            );

        var columnName = GetSubqueryOutputColumnName(leafQuery.Select.Fields[0]);
        var leftSide = NormalizeSubqueryLeftSide(inQueryNode.Left);
        var leftNeedsRename = leftSide is AccessColumnNode { Alias: null or "" };
        var cteColumnName = leftNeedsRename ? GeneratedSubqueryContract.CreateKeyColumnName(cteName) : columnName;
        var cteOutputNeedsRename = leftNeedsRename || ShouldRenameSubqueryOutput(leafQuery.Select.Fields[0]);

        Node cteBody;
        Node? correlationJoinExpression = null;
        if (subqueryBody is QueryNode queryNode)
        {
            var (rewrittenSubquery, innerCtes) = RewriteNestedInSubqueries(queryNode);
            cteInnerExpressions.AddRange(innerCtes);
            var correlatedSubquery = RewriteCorrelatedSubqueryIfNeeded(rewrittenSubquery, subqueryInfo, cteName);
            rewrittenSubquery = correlatedSubquery.Query;
            correlationJoinExpression = correlatedSubquery.JoinPredicate;

            cteBody = cteOutputNeedsRename ? RenameSelectColumn(rewrittenSubquery, cteColumnName) : rewrittenSubquery;
        }
        else
        {
            cteBody = subqueryBody;
        }

        return new SubqueryJoinRewrite(
            cteBody,
            leftSide,
            cteColumnName,
            correlationJoinExpression,
            leafQuery.Select.Fields[0].Expression
        );
    }

    private SubqueryJoinRewrite PrepareExistsSubquery(
        SubqueryInfo subqueryInfo,
        Node subqueryBody,
        string cteName,
        List<CteInnerExpressionNode> cteInnerExpressions
    )
    {
        var cteColumnName = subqueryBody
            is QueryNode { Select.Fields: [{ FieldName: GeneratedSubqueryContract.GreaterAllKeySuffix }] }
            ? cteName + GeneratedSubqueryContract.GreaterAllKeySuffix
            : GeneratedSubqueryContract.CreateKeyColumnName(cteName);
        var leftSide = new IntegerNode(1);
        Node cteBody;
        Node? correlationJoinExpression = null;

        if (subqueryBody is QueryNode queryNode)
        {
            var (rewrittenSubquery, innerCtes) = RewriteNestedInSubqueries(queryNode);
            cteInnerExpressions.AddRange(innerCtes);
            rewrittenSubquery = ProjectExistsKey(rewrittenSubquery, cteColumnName);

            var correlatedSubquery = RewriteCorrelatedSubqueryIfNeeded(rewrittenSubquery, subqueryInfo, cteName);
            rewrittenSubquery = correlatedSubquery.Query;
            correlationJoinExpression = correlatedSubquery.JoinPredicate;

            cteBody = rewrittenSubquery;
        }
        else
        {
            var result = CreateUniqueSubqueryName();
            cteInnerExpressions.Add(new CteInnerExpressionNode(subqueryBody, result));
            cteBody = new QueryNode(
                new SelectNode([new FieldNode(new IntegerNode(1), 0, cteColumnName)]),
                new Parser.ExpressionFromNode(new Parser.InMemoryTableFromNode(result, result)),
                null,
                null,
                null,
                null,
                null
            );
        }

        return new SubqueryJoinRewrite(
            cteBody,
            leftSide,
            cteColumnName,
            correlationJoinExpression,
            subqueryInfo.PredicateNode
        );
    }

    private sealed record SubqueryJoinRewrite(
        Node CteBody,
        Node LeftSide,
        string CteColumnName,
        Node? CorrelationJoinExpression,
        Node DiagnosticNode
    );

    private sealed record PredicateSubqueryJoin(
        InMemoryTableFromNode CteRef,
        Node JoinExpression,
        Node Replacement,
        SubqueryJoinRewrite? Membership = null
    );
}
