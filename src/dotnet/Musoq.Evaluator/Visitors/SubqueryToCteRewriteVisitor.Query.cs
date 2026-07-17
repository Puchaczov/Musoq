using System.Collections.Generic;
using Musoq.Evaluator.IR.Optimization.Logical.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    public override void Visit(QueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var orderBy = node.OrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var qualify = node.Qualify != null ? Nodes.Pop() as QualifyNode : null;
        var window = node.Window != null ? Nodes.Pop() as WindowNode : null;
        var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;

        var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
        var take = node.Take != null ? Nodes.Pop() as TakeNode : null;

        var select = SafeCast<SelectNode>(Nodes.Pop(), nameof(Visit));
        var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
        var from = SafeCast<FromNode>(Nodes.Pop(), nameof(Visit));

        var sourceQuery = new QueryNode(select, from, where, groupBy, orderBy, skip, take, window, qualify, default);
        RegisterReservedAliases(CollectFromAliases(from));
        var analysis = AnalyzeSubqueries(sourceQuery);
        ValidateSubqueryAnalysis(analysis);
        RegisterRewriteRequests(CorrelatedSubqueryRewriteRequestBuilder.Build(sourceQuery, analysis));
        var (remainingExpr, subqueries) = where != null
            ? AttachCorrelation(ExtractPredicateSubqueries(where.Expression), analysis)
            : (null, []);
        var cteInnerExpressions = new List<CteInnerExpressionNode>();
        var currentFrom = UnwrapFromForJoin(from);
        currentFrom = RewriteDerivedTablesToCtes(currentFrom, cteInnerExpressions);

        if (subqueries.Count > 0)
        {
            (currentFrom, remainingExpr) = RewriteSubqueriesToJoins(
                subqueries, currentFrom, remainingExpr, cteInnerExpressions);
        }

        if (groupBy?.Having != null)
        {
            var (havingExpression, havingSubqueries) = AttachCorrelation(
                ExtractPredicateSubqueries(groupBy.Having.Expression),
                analysis);

            if (havingSubqueries.Count > 0)
            {
                ValidateHavingPredicateSubqueriesCanMoveBeforeGrouping(havingSubqueries, groupBy);
                (currentFrom, havingExpression) = RewriteSubqueriesToJoins(
                    havingSubqueries, currentFrom, havingExpression, cteInnerExpressions);
                groupBy = new GroupByNode(
                    groupBy.Fields,
                    havingExpression != null ? new HavingNode(havingExpression) : null,
                    groupBy.IsAll,
                    groupBy.Span);
            }
        }

        var predicateApplyRewrite = RewritePredicateApplySubqueries(
            select,
            currentFrom,
            remainingExpr,
            groupBy,
            orderBy,
            window,
            qualify,
            analysis,
            cteInnerExpressions);
        select = predicateApplyRewrite.Select;
        currentFrom = predicateApplyRewrite.From;
        remainingExpr = predicateApplyRewrite.WhereExpression;
        groupBy = predicateApplyRewrite.GroupBy;
        orderBy = predicateApplyRewrite.OrderBy;
        window = predicateApplyRewrite.Window;
        qualify = predicateApplyRewrite.Qualify;

        var scalarRewrite = RewriteScalarSubqueries(
            select,
            currentFrom,
            remainingExpr,
            groupBy,
            orderBy,
            window,
            qualify,
            analysis,
            cteInnerExpressions);
        select = scalarRewrite.Select;
        currentFrom = scalarRewrite.From;
        remainingExpr = scalarRewrite.WhereExpression;
        groupBy = scalarRewrite.GroupBy;
        orderBy = scalarRewrite.OrderBy;
        window = scalarRewrite.Window;
        qualify = scalarRewrite.Qualify;

        if (cteInnerExpressions.Count == 0)
        {
            Nodes.Push(new QueryNode(select, from, where, groupBy, orderBy, skip, take, window, qualify, default));
            return;
        }

        var wrappedFrom = WrapJoinedFrom(currentFrom);
        var newWhere = remainingExpr != null ? new WhereNode(remainingExpr) : null;
        var newQuery = new QueryNode(select, wrappedFrom, newWhere, groupBy, orderBy, skip, take, window, qualify,
            default);

        Nodes.Push(new CteExpressionNode(cteInnerExpressions.ToArray(), newQuery));
    }
}
