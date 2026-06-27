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
                JoinType.OuterLeft));

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
            if (correlation is { IsCorrelated: true })
                throw SubqueryDiagnosticFactory.InvalidSubquery(
                    "scalar subquery validation",
                    "Correlated scalar subqueries over set operators require APPLY fallback lowering and are not supported yet.",
                    node);

            scalarRewrite = RewriteMaterializedUncorrelatedScalarSubquery(
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
                ? RewriteCorrelatedScalarSubquery(rewrittenSubquery, correlation, cteName, valueColumnName, node)
                : RewriteUncorrelatedScalarSubquery(
                    rewrittenSubquery,
                    cteName,
                    keyColumnName,
                    valueColumnName,
                    cteInnerExpressions);
        }

        cteInnerExpressions.Add(new CteInnerExpressionNode(scalarRewrite.Query, cteName));

        var replacement = new AccessColumnNode(valueColumnName, cteName, default);
        var cteRef = new Parser.InMemoryTableFromNode(cteName, cteName);
        return new ScalarSubqueryJoin(cteRef, scalarRewrite.JoinExpression, replacement);
    }

    private static ScalarSubqueryRewrite RewriteCorrelatedScalarSubquery(
        QueryNode query,
        SubqueryCorrelationInfo correlation,
        string cteName,
        string valueColumnName,
        ScalarSubqueryNode node)
    {
        var valueExpression = query.Select.Fields[0].Expression;
        var valueContainsAggregate = ContainsAggregateMethod(valueExpression);
        if (RequiresCorrelatedAggregateFallback(query))
            ThrowUnsupportedCorrelatedScalarResultMaterialization(node);

        var where = query.Where;
        if (where == null)
            ThrowUnsupportedScalarCorrelation(node);

        var conjuncts = SplitConjuncts(where.Expression);
        var correlated = conjuncts
            .Where(predicate => ReferencesAnyAlias(predicate, correlation.CorrelatedAliases))
            .ToArray();

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
            valueContainsAggregate ? valueExpression : CreateScalarAggregate(valueExpression),
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

    private static bool RequiresResultMaterialization(QueryNode query, Node valueExpression)
    {
        return ContainsAggregateMethod(valueExpression) ||
               query.GroupBy != null ||
               query.OrderBy != null ||
               query.Skip != null ||
               query.Take != null ||
               query.Window != null ||
               query.Qualify != null ||
               query.Select.IsDistinct;
    }

    private static bool RequiresCorrelatedAggregateFallback(QueryNode query)
    {
        return query.GroupBy != null ||
               query.OrderBy != null ||
               query.Skip != null ||
               query.Take != null ||
               query.Window != null ||
               query.Qualify != null ||
               query.Select.IsDistinct;
    }

    private static bool ContainsAggregateMethod(Node expression)
    {
        var detector = new AggregateMethodDetector();
        expression.Accept(new AggregateMethodTraverser(detector));
        return detector.Found;
    }

    private static bool IsAggregateMethodName(string name)
    {
        return name.Equals("AggregateValues", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Avg", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Count", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Max", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Min", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("StDev", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Sum", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("SumIncome", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("SumOutcome", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Window", StringComparison.OrdinalIgnoreCase);
    }

    private static AccessMethodNode CreateScalarAggregate(Node expression)
    {
        return new AccessMethodNode(
            new FunctionToken(ScalarSubqueryAggregateName, default),
            new ArgsListNode([expression]),
            null,
            false);
    }

    [DoesNotReturn]
    private static void ThrowUnsupportedScalarCorrelation(ScalarSubqueryNode node)
    {
        throw SubqueryDiagnosticFactory.InvalidSubquery(
            "correlated scalar subquery rewrite",
            "Correlated scalar subqueries currently require equality predicates in the subquery WHERE clause.",
            node);
    }

    [DoesNotReturn]
    private static void ThrowUnsupportedCorrelatedScalarResultMaterialization(ScalarSubqueryNode node)
    {
        throw SubqueryDiagnosticFactory.InvalidSubquery(
            "correlated scalar subquery result materialization",
            "Correlated scalar subqueries with DISTINCT, GROUP BY, ORDER BY, SKIP, TAKE, WINDOW, or QUALIFY inside the subquery body require APPLY fallback lowering and are not supported yet.",
            node);
    }

}
