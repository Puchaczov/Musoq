using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.Helpers.Subqueries.SubqueryCorrelationUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private static bool TryRewriteRangeCorrelatedScalarSubquery(
        QueryNode query,
        SubqueryCorrelationInfo correlation,
        string cteName,
        string valueColumnName,
        Node valueExpression,
        Node[] conjuncts,
        Node[] correlated,
        out ScalarSubqueryRewrite rewrite)
    {
        rewrite = null!;
        if (correlated.Length == 0 ||
            correlated.Count(IsSingleRangeCorrelation) != 1 ||
            correlated.Any(static predicate => predicate is not EqualityNode && !IsSingleRangeCorrelation(predicate)) ||
            BuildMetadataAndInferTypesVisitorUtilities.ContainsAggregateFunction(valueExpression))
            return false;

        rewrite = RewriteRangeCorrelatedScalarSubquery(
            query,
            correlation,
            cteName,
            valueColumnName,
            correlated,
            conjuncts.Where(predicate => !correlated.Contains(predicate)).ToArray());
        return true;
    }

    private static ScalarSubqueryRewrite RewriteRangeCorrelatedScalarSubquery(
        QueryNode query,
        SubqueryCorrelationInfo correlation,
        string cteName,
        string valueColumnName,
        Node[] correlated,
        Node[] local)
    {
        var projections = CollectCorrelationProjections(correlated, correlation.LocalAliases, cteName);
        if (projections.Length == 0)
            throw new InvalidOperationException("Range-correlated scalar lowering requires one projected inner key.");

        var fields = new FieldNode[projections.Length + 1];
        for (var index = 0; index < projections.Length; index++)
        {
            var projection = projections[index];
            fields[index] = new FieldNode(
                new AccessColumnNode(
                    projection.ColumnName,
                    projection.Alias,
                    projection.ReturnType ?? typeof(object),
                    projection.Span,
                    projection.IntendedTypeName),
                index,
                projection.CteColumnName);
        }

        fields[^1] = new FieldNode(query.Select.Fields[0].Expression, fields.Length - 1, valueColumnName);
        var rewritten = new QueryNode(
            new SelectNode(fields),
            query.From,
            CombineConjuncts(local) is { } localWhere ? new WhereNode(localWhere) : null,
            null,
            null,
            null,
            null,
            null,
            null,
            default);
        var joinPredicate = RewriteCorrelatedPredicatesForJoin(
            correlated,
            projections,
            correlation.LocalAliases,
            cteName);

        return new ScalarSubqueryRewrite(rewritten, joinPredicate);
    }
}
