using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.Helpers.Subqueries.SubqueryCorrelationUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private ScalarSubqueryRewrite RewriteCorrelatedScalarSetSubquery(
        Node body,
        SubqueryCorrelationInfo correlation,
        string cteName,
        string valueColumnName,
        ScalarSubqueryNode scalarNode,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        var materializedCteName = CreateScalarMaterializationCteName(cteName);
        var materializedValueColumnName = GeneratedSubqueryContract.CreateValueColumnName(materializedCteName);
        var setRewrite = RewriteCorrelatedScalarSetBody(
            body,
            correlation,
            cteName,
            materializedValueColumnName,
            scalarNode);
        cteInnerExpressions.Add(new CteInnerExpressionNode(setRewrite.Body, materializedCteName));

        var carrierQuery = CreateCorrelatedScalarCarrierQuery(
            materializedCteName,
            setRewrite.Projections,
            materializedValueColumnName,
            valueColumnName);
        return new ScalarSubqueryRewrite(carrierQuery, setRewrite.JoinPredicate);
    }

    private static CorrelatedScalarSetRewrite RewriteCorrelatedScalarSetBody(
        Node body,
        SubqueryCorrelationInfo correlation,
        string cteName,
        string materializedValueColumnName,
        ScalarSubqueryNode scalarNode)
    {
        switch (body)
        {
            case QueryNode query:
                return RewriteCorrelatedScalarSetBranch(
                    query,
                    correlation,
                    cteName,
                    materializedValueColumnName,
                    scalarNode);
            case SingleSetNode singleSet:
            {
                var rewrite = RewriteCorrelatedScalarSetBranch(
                    singleSet.Query,
                    correlation,
                    cteName,
                    materializedValueColumnName,
                    scalarNode);
                return rewrite with { Body = new SingleSetNode((QueryNode)rewrite.Body) };
            }
            case SetOperatorNode setOperator:
            {
                var left = RewriteCorrelatedScalarSetBody(
                    setOperator.Left,
                    correlation,
                    cteName,
                    materializedValueColumnName,
                    scalarNode);
                var right = RewriteCorrelatedScalarSetBody(
                    setOperator.Right,
                    correlation,
                    cteName,
                    materializedValueColumnName,
                    scalarNode);
                RequireCompatibleCorrelatedScalarSetBranches(left, right, scalarNode);
                var setKeys = left.Projections
                    .Select(static projection => projection.CteColumnName)
                    .Append(materializedValueColumnName)
                    .ToArray();
                return left with
                {
                    Body = RecreateCorrelatedScalarSetOperator(setOperator, left.Body, right.Body, setKeys)
                };
            }
            default:
                throw SubqueryDiagnosticFactory.InvalidSubquery(
                    "correlated scalar set rewrite",
                    "Correlated scalar set operators require query branches with a shared equality correlation key.",
                    scalarNode);
        }
    }

    private static CorrelatedScalarSetRewrite RewriteCorrelatedScalarSetBranch(
        QueryNode query,
        SubqueryCorrelationInfo correlation,
        string cteName,
        string materializedValueColumnName,
        ScalarSubqueryNode scalarNode)
    {
        if (query.Select.Fields.Length != 1)
            throw SubqueryDiagnosticFactory.InvalidSubquery(
                "correlated scalar set rewrite",
                "Every scalar set-operator branch must return exactly one column.",
                scalarNode);
        if (RequiresUnsupportedCombinedScalarShape(query))
            ThrowUnsupportedCorrelatedScalarResultMaterialization(scalarNode);
        if (query.Where == null)
            ThrowUnsupportedCorrelatedScalarSetBranch(scalarNode);

        var conjuncts = SplitConjuncts(query.Where.Expression);
        var correlated = conjuncts
            .Where(predicate => ReferencesAnyAlias(predicate, correlation.CorrelatedAliases))
            .ToArray();
        if (correlated.Length == 0 || correlated.Any(predicate => predicate is not EqualityNode))
            ThrowUnsupportedCorrelatedScalarSetBranch(scalarNode);

        var projections = CollectCorrelationProjections(correlated, correlation.LocalAliases, cteName);
        if (projections.Length == 0)
            ThrowUnsupportedCorrelatedScalarSetBranch(scalarNode);

        var local = conjuncts.Where(predicate => !correlated.Contains(predicate)).ToArray();
        var rewritten = CreateCorrelatedScalarMaterializationQuery(
            query,
            projections,
            materializedValueColumnName,
            local);
        var joinPredicate = RewriteCorrelatedPredicatesForJoin(
            correlated,
            projections,
            correlation.LocalAliases,
            cteName);
        return new CorrelatedScalarSetRewrite(rewritten, projections, joinPredicate);
    }

    private static void RequireCompatibleCorrelatedScalarSetBranches(
        CorrelatedScalarSetRewrite left,
        CorrelatedScalarSetRewrite right,
        ScalarSubqueryNode scalarNode)
    {
        var sameProjectionContract = left.Projections.Length == right.Projections.Length &&
                                     left.Projections.Zip(right.Projections).All(static pair =>
                                         string.Equals(
                                             pair.First.CteColumnName,
                                             pair.Second.CteColumnName,
                                             StringComparison.OrdinalIgnoreCase));
        if (sameProjectionContract &&
            string.Equals(left.JoinPredicate.ToString(), right.JoinPredicate.ToString(), StringComparison.Ordinal))
        {
            return;
        }

        throw SubqueryDiagnosticFactory.InvalidSubquery(
            "correlated scalar set rewrite",
            "Every correlated scalar set-operator branch must expose the same equality correlation key.",
            scalarNode);
    }

    private static SetOperatorNode RecreateCorrelatedScalarSetOperator(
        SetOperatorNode node,
        Node left,
        Node right,
        string[] keys)
    {
        return node switch
        {
            UnionNode => new UnionNode(node.ResultTableName, keys, left, right, node.IsNested, node.IsTheLastOne,
                node.ResultOrderBy, node.ResultSkip, node.ResultTake)
            {
                KeySpans = node.KeySpans
            },
            UnionAllNode => new UnionAllNode(node.ResultTableName, keys, left, right, node.IsNested, node.IsTheLastOne,
                node.ResultOrderBy, node.ResultSkip, node.ResultTake)
            {
                KeySpans = node.KeySpans
            },
            ExceptNode => new ExceptNode(node.ResultTableName, keys, left, right, node.IsNested, node.IsTheLastOne,
                node.ResultOrderBy, node.ResultSkip, node.ResultTake)
            {
                KeySpans = node.KeySpans
            },
            IntersectNode => new IntersectNode(node.ResultTableName, keys, left, right, node.IsNested, node.IsTheLastOne,
                node.ResultOrderBy, node.ResultSkip, node.ResultTake)
            {
                KeySpans = node.KeySpans
            },
            _ => throw new InvalidOperationException($"Unsupported scalar set operator {node.GetType().Name}.")
        };
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void ThrowUnsupportedCorrelatedScalarSetBranch(ScalarSubqueryNode scalarNode)
    {
        throw SubqueryDiagnosticFactory.InvalidSubquery(
            "correlated scalar set rewrite",
            "Every correlated scalar set-operator branch must contain equality correlation predicates in its WHERE clause.",
            scalarNode);
    }

    private sealed record CorrelatedScalarSetRewrite(
        Node Body,
        CorrelationProjection[] Projections,
        Node JoinPredicate);
}
