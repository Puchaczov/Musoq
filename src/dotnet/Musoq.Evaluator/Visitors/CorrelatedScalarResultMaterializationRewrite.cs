using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using static Musoq.Evaluator.Visitors.Helpers.Subqueries.SubqueryCorrelationUtilities;

namespace Musoq.Evaluator.Visitors;

// Keeps correlated scalar result shaping outside the central rewrite family size budget.
public partial class SubqueryToCteRewriteVisitor
{
    private ScalarSubqueryRewrite RewriteMaterializedCorrelatedScalarSubquery(
        QueryNode query,
        SubqueryCorrelationInfo correlation,
        string cteName,
        string valueColumnName,
        ScalarSubqueryNode node,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        var where = query.Where;
        if (where == null)
            ThrowUnsupportedScalarCorrelation(node);

        var conjuncts = SplitConjuncts(where.Expression);
        var correlated = conjuncts
            .Where(predicate => ReferencesAnyAlias(predicate, correlation.CorrelatedAliases))
            .ToArray();
        if (correlated.Length == 0 || correlated.Any(predicate => predicate is not EqualityNode))
            ThrowUnsupportedScalarCorrelation(node);

        var projections = CollectCorrelationProjections(correlated, correlation.LocalAliases, cteName);
        if (projections.Length == 0)
            ThrowUnsupportedScalarCorrelation(node);

        var materializedCteName = CreateScalarMaterializationCteName(cteName);
        var materializedValueColumnName = GeneratedSubqueryContract.CreateValueColumnName(materializedCteName);
        var local = conjuncts.Where(predicate => !correlated.Contains(predicate)).ToArray();
        var materializedQuery = CreateCorrelatedScalarMaterializationQuery(
            query,
            projections,
            materializedValueColumnName,
            local);
        cteInnerExpressions.Add(new CteInnerExpressionNode(materializedQuery, materializedCteName));

        var rewritten = CreateCorrelatedScalarCarrierQuery(
            materializedCteName,
            projections,
            materializedValueColumnName,
            valueColumnName);
        var joinPredicate = RewriteCorrelatedPredicatesForJoin(
            correlated,
            projections,
            correlation.LocalAliases,
            cteName);

        return new ScalarSubqueryRewrite(rewritten, joinPredicate);
    }

    private static QueryNode CreateCorrelatedScalarCarrierQuery(
        string materializedCteName,
        IReadOnlyList<CorrelationProjection> projections,
        string materializedValueColumnName,
        string valueColumnName)
    {
        var fields = new FieldNode[projections.Count + 1];
        var groupByFields = new FieldNode[projections.Count];
        for (var i = 0; i < projections.Count; i++)
        {
            var key = new AccessColumnNode(
                projections[i].CteColumnName,
                materializedCteName,
                projections[i].ReturnType ?? typeof(object),
                projections[i].Span,
                projections[i].IntendedTypeName);
            fields[i] = new FieldNode(key, i, projections[i].CteColumnName);
            groupByFields[i] = new FieldNode(key, i, string.Empty);
        }

        fields[^1] = new FieldNode(
            CreateDeferredCorrelatedScalarAggregate(new AccessColumnNode(
                materializedValueColumnName,
                materializedCteName,
                default)),
            fields.Length - 1,
            valueColumnName);
        return new QueryNode(
            new SelectNode(fields),
            new Parser.ExpressionFromNode(
                new Parser.InMemoryTableFromNode(materializedCteName, materializedCteName)),
            null,
            new GroupByNode(groupByFields, null),
            null,
            null,
            null,
            null,
            null,
            default);
    }

    private static QueryNode CreateCorrelatedScalarMaterializationQuery(
        QueryNode query,
        IReadOnlyList<CorrelationProjection> projections,
        string materializedValueColumnName,
        IReadOnlyList<Node> localPredicates)
    {
        var fields = new FieldNode[projections.Count + 1];
        var partitionFields = new FieldNode[projections.Count];
        for (var i = 0; i < projections.Count; i++)
        {
            var projection = projections[i];
            var key = new AccessColumnNode(
                projection.ColumnName,
                projection.Alias,
                projection.ReturnType ?? typeof(object),
                projection.Span,
                projection.IntendedTypeName);
            fields[i] = new FieldNode(key, i, projection.CteColumnName);
            partitionFields[i] = new FieldNode(key, i, string.Empty);
        }

        var windowRewriter = new CorrelationWindowPartitionRewriter(partitionFields);
        fields[^1] = new FieldNode(
            windowRewriter.Rewrite(query.Select.Fields[0].Expression),
            fields.Length - 1,
            materializedValueColumnName);
        var groupBy = CreateCorrelatedScalarGroupBy(query.GroupBy, partitionFields, windowRewriter);
        var orderBy = query.OrderBy == null ? null : windowRewriter.Rewrite(query.OrderBy);
        var qualify = query.Qualify == null ? null : windowRewriter.Rewrite(query.Qualify);
        var window = query.Window == null ? null : windowRewriter.Rewrite(query.Window);
        var sliceQualify = CreatePartitionedTopOffsetQualify(
            partitionFields,
            orderBy?.Fields ?? [],
            query.Skip?.Value ?? 0L,
            query.Take?.Value);
        if (sliceQualify != null)
        {
            qualify = qualify == null
                ? sliceQualify
                : new QualifyNode(new AndNode(qualify.Expression, sliceQualify.Expression));
        }

        return new QueryNode(
            new SelectNode(fields, query.Select.IsDistinct),
            query.From,
            CombineConjuncts(localPredicates) is { } localWhere ? new WhereNode(localWhere) : null,
            groupBy,
            null,
            null,
            null,
            window,
            qualify,
            default);
    }

    private static GroupByNode? CreateCorrelatedScalarGroupBy(
        GroupByNode? groupBy,
        FieldNode[] partitionFields,
        CorrelationWindowPartitionRewriter windowRewriter)
    {
        if (groupBy == null)
            return null;

        var rewritten = windowRewriter.Rewrite(groupBy);
        if (rewritten.IsAll)
            return rewritten;

        var fields = new FieldNode[partitionFields.Length + rewritten.Fields.Length];
        for (var i = 0; i < partitionFields.Length; i++)
            fields[i] = partitionFields[i];
        for (var i = 0; i < rewritten.Fields.Length; i++)
            fields[partitionFields.Length + i] = rewritten.Fields[i];
        return new GroupByNode(fields, rewritten.Having, rewritten.IsAll);
    }

    private static QualifyNode? CreatePartitionedTopOffsetQualify(
        FieldNode[] partitionFields,
        FieldOrderedNode[] orderByFields,
        long skip,
        long? take)
    {
        if (skip == 0 && take == null)
            return null;

        Node? predicate = skip > 0
            ? new GreaterNode(CreatePartitionRowNumber(partitionFields, orderByFields), new IntegerNode(skip))
            : null;
        if (take is { } limit)
        {
            var upperBound = limit > long.MaxValue - skip ? long.MaxValue : skip + limit;
            predicate = skip > 0
                ? new BetweenNode(
                    CreatePartitionRowNumber(partitionFields, orderByFields),
                    new IntegerNode(skip + 1),
                    new IntegerNode(upperBound))
                : new LessOrEqualNode(
                    CreatePartitionRowNumber(partitionFields, orderByFields),
                    new IntegerNode(upperBound));
        }

        return new QualifyNode(predicate ?? throw new InvalidOperationException(
            "Partitioned scalar slicing requires an offset or limit predicate."));
    }

    private static WindowFunctionNode CreatePartitionRowNumber(
        FieldNode[] partitionFields,
        FieldOrderedNode[] orderByFields)
    {
        return new WindowFunctionNode(
            new AccessMethodNode(
                new FunctionToken("RowNumber", default),
                new ArgsListNode([]),
                null,
                false),
            new WindowSpecificationNode(partitionFields, orderByFields));
    }
}
