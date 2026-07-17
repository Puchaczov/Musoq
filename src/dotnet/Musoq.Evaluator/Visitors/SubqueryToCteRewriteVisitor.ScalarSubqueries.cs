using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using static Musoq.Evaluator.Visitors.Helpers.Subqueries.SubqueryCorrelationUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private const string ScalarSubqueryAggregateName = "__ScalarSubqueryValue";
    private const string CorrelatedScalarSubqueryAggregateName = "__CorrelatedScalarSubqueryValue";
    private const string CorrelatedScalarSubqueryResultName = "__CorrelatedScalarSubqueryResult";

    private ScalarRewriteResult RewriteScalarSubqueries(
        SelectNode select,
        FromNode from,
        Node? whereExpression,
        GroupByNode? groupBy,
        OrderByNode? orderBy,
        WindowNode? window,
        QualifyNode? qualify,
        SubqueryCorrelationAnalysis analysis,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        var rewriter = new ScalarSubqueryExpressionRewriter(this, analysis, cteInnerExpressions);
        var context = RewriteExpressionContext(
            new SubqueryRewriteContext(select, from, whereExpression, groupBy, orderBy, window, qualify),
            rewriter,
            static (currentFrom, join) => new Parser.JoinFromNode(
                currentFrom,
                join.CteRef,
                join.JoinExpression,
                join.JoinType));

        return new ScalarRewriteResult(
            context.Select,
            context.From,
            context.WhereExpression,
            context.GroupBy,
            context.OrderBy,
            context.Window,
            context.Qualify);
    }

    private ScalarSubqueryJoin PrepareScalarSubquery(
        ScalarSubqueryNode node,
        SubqueryCorrelationAnalysis analysis,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        var cteName = CreateUniqueSubqueryName();
        var valueColumnName = GeneratedSubqueryContract.CreateValueColumnName(cteName);
        var keyColumnName = GeneratedSubqueryContract.CreateKeyColumnName(cteName);
        var subqueryBody = UnwrapCteSubqueryBody(node.Subquery, cteInnerExpressions);
        var leafQuery = GetLeftmostQuery(subqueryBody);
        var correlation = FindCorrelation(node, analysis);

        if (leafQuery.Select.Fields.Length != 1)
            throw SubqueryDiagnosticFactory.InvalidSubquery(
                "scalar subquery validation",
                "Scalar subquery must return exactly one column.",
                node);

        var queryNode = subqueryBody switch
        {
            QueryNode query => query,
            SingleSetNode singleSet => singleSet.Query,
            _ => null
        };

        ScalarSubqueryRewrite scalarRewrite;
        if (queryNode == null)
        {
            scalarRewrite = correlation is { IsCorrelated: true }
                ? RewriteCorrelatedScalarSetSubquery(
                    subqueryBody,
                    correlation,
                    cteName,
                    valueColumnName,
                    node,
                    cteInnerExpressions)
                : RewriteMaterializedUncorrelatedScalarSubquery(
                    subqueryBody,
                    GetSubqueryOutputColumnName(leafQuery.Select.Fields[0]),
                    cteName,
                    keyColumnName,
                    valueColumnName,
                    cteInnerExpressions);
        }
        else
        {
            var (rewrittenSubquery, innerCtes) = RewriteNestedInSubqueries(queryNode);
            cteInnerExpressions.AddRange(innerCtes);

            scalarRewrite = correlation is { IsCorrelated: true }
                ? RewriteCorrelatedScalarSubquery(
                    rewrittenSubquery,
                    correlation,
                    cteName,
                    valueColumnName,
                    node,
                    cteInnerExpressions)
                : RewriteUncorrelatedScalarSubquery(
                    rewrittenSubquery,
                    cteName,
                    keyColumnName,
                    valueColumnName,
                    cteInnerExpressions);
        }

        cteInnerExpressions.Add(new CteInnerExpressionNode(scalarRewrite.Query, cteName));

        Node replacement = new AccessColumnNode(valueColumnName, cteName, default);
        var joinType = JoinType.OuterLeft;
        if (correlation is { IsCorrelated: true })
        {
            replacement = CreateCorrelatedScalarResultAccessor(
                replacement,
                correlation.CorrelatedAliases.Order(StringComparer.OrdinalIgnoreCase).First());
            joinType = JoinType.LeftSingle;
        }

        var cteRef = new Parser.InMemoryTableFromNode(cteName, cteName);
        return new ScalarSubqueryJoin(cteRef, scalarRewrite.JoinExpression, replacement, joinType);
    }

    private ScalarSubqueryRewrite RewriteCorrelatedScalarSubquery(
        QueryNode query,
        SubqueryCorrelationInfo correlation,
        string cteName,
        string valueColumnName,
        ScalarSubqueryNode node,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        var valueExpression = query.Select.Fields[0].Expression;
        if (RequiresResultMaterialization(query) &&
            !RequiresUnsupportedCombinedScalarShape(query))
        {
            return RewriteMaterializedCorrelatedScalarSubquery(
                query,
                correlation,
                cteName,
                valueColumnName,
                node,
                cteInnerExpressions);
        }

        if (RequiresCorrelatedAggregateApply(query))
            ThrowUnsupportedCorrelatedScalarResultMaterialization(node);

        var where = query.Where;
        if (where == null)
            ThrowUnsupportedScalarCorrelation(node);

        var conjuncts = SplitConjuncts(where.Expression);
        var correlated = conjuncts
            .Where(predicate => ReferencesAnyAlias(predicate, correlation.CorrelatedAliases))
            .ToArray();

        if (TryRewriteRangeCorrelatedScalarSubquery(
                query, correlation, cteName, valueColumnName, valueExpression, conjuncts, correlated, out var rangeRewrite))
            return rangeRewrite;

        if (correlated.Length == 0 || correlated.Any(predicate => predicate is not EqualityNode))
            ThrowUnsupportedScalarCorrelation(node);

        var local = conjuncts
            .Where(predicate => !correlated.Contains(predicate))
            .ToArray();
        var projections = CollectCorrelationProjections(correlated, correlation.LocalAliases, cteName);
        if (projections.Length == 0)
            ThrowUnsupportedScalarCorrelation(node);

        var fields = new FieldNode[projections.Length + 1];
        var groupByFields = new FieldNode[projections.Length];

        for (var i = 0; i < projections.Length; i++)
        {
            var projection = projections[i];
            var access = new AccessColumnNode(
                projection.ColumnName,
                projection.Alias,
                projection.ReturnType ?? typeof(object),
                projection.Span,
                projection.IntendedTypeName);
            fields[i] = new FieldNode(access, i, projection.CteColumnName);
            groupByFields[i] = new FieldNode(access, i, string.Empty);
        }

        fields[^1] = new FieldNode(
            CreateDeferredCorrelatedScalarAggregate(valueExpression),
            fields.Length - 1,
            valueColumnName);

        var rewritten = new QueryNode(
            new SelectNode(fields),
            query.From,
            CombineConjuncts(local) is { } localWhere ? new WhereNode(localWhere) : null,
            new GroupByNode(groupByFields, null),
            query.OrderBy,
            query.Skip,
            query.Take,
            query.Window,
            query.Qualify,
            default);
        var joinPredicate = RewriteCorrelatedPredicatesForJoin(
            correlated,
            projections,
            correlation.LocalAliases,
            cteName);

        return new ScalarSubqueryRewrite(rewritten, joinPredicate);
    }

    private static AccessMethodNode CreateScalarAggregate(Node expression)
    {
        return new AccessMethodNode(
            new FunctionToken(ScalarSubqueryAggregateName, default),
            new ArgsListNode([expression]),
            null,
            false);
    }

    private static AccessMethodNode CreateDeferredScalarAggregate(Node expression)
    {
        return new AccessMethodNode(
            new FunctionToken(ScalarSubqueryAggregateName, default),
            new ArgsListNode([expression]),
            null,
            false)
        {
            IsScalarSubqueryValueWrapper = true
        };
    }

    [DoesNotReturn]
    private static void ThrowUnsupportedScalarCorrelation(ScalarSubqueryNode node)
    {
        throw SubqueryDiagnosticFactory.InvalidSubquery(
            "correlated scalar subquery rewrite",
            "Correlated scalar subqueries require equality predicates with at most one comparable range predicate in the subquery WHERE clause. Range-correlated aggregate forms are not supported.",
            node);
    }

    [DoesNotReturn]
    private static void ThrowUnsupportedCorrelatedScalarResultMaterialization(ScalarSubqueryNode node)
    {
        throw SubqueryDiagnosticFactory.InvalidSubquery(
            "correlated scalar subquery result materialization",
            "Correlated scalar subqueries that combine SKIP or TAKE with DISTINCT, GROUP BY, WINDOW, or QUALIFY require a post-shaping partition stage and are not supported yet.",
            node);
    }

}
