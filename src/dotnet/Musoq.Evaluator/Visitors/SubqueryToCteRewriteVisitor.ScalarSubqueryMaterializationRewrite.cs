using System.Collections.Generic;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private ScalarSubqueryRewrite RewriteUncorrelatedScalarSubquery(
        QueryNode query,
        string cteName,
        string keyColumnName,
        string valueColumnName,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        var valueExpression = query.Select.Fields[0].Expression;
        var valueContainsAggregate = ContainsAggregateMethod(valueExpression);
        if (RequiresResultMaterialization(query, valueExpression) &&
            !(valueContainsAggregate && !RequiresCorrelatedAggregateFallback(query)))
        {
            return RewriteMaterializedUncorrelatedScalarSubquery(
                query,
                cteName,
                keyColumnName,
                valueColumnName,
                cteInnerExpressions);
        }

        var fields = new[]
        {
            new FieldNode(new IntegerNode(1), 0, keyColumnName),
            new FieldNode(
                valueContainsAggregate ? valueExpression : CreateScalarAggregate(valueExpression),
                1,
                valueColumnName)
        };

        var rewritten = new QueryNode(
            new SelectNode(fields),
            query.From,
            query.Where,
            null,
            query.OrderBy,
            query.Skip,
            query.Take,
            query.Window,
            query.Qualify,
            default);

        return new ScalarSubqueryRewrite(
            rewritten,
            new EqualityNode(new IntegerNode(1), new AccessColumnNode(keyColumnName, cteName, default)));
    }

    private ScalarSubqueryRewrite RewriteMaterializedUncorrelatedScalarSubquery(
        QueryNode query,
        string cteName,
        string keyColumnName,
        string valueColumnName,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        var materializedCteName = CreateScalarMaterializationCteName(cteName);
        var materializedValueColumnName = GeneratedSubqueryContract.CreateValueColumnName(materializedCteName);
        var materializedQuery = RenameSelectColumn(query, materializedValueColumnName);
        return RewriteMaterializedUncorrelatedScalarSubquery(
            materializedQuery,
            materializedValueColumnName,
            cteName,
            keyColumnName,
            valueColumnName,
            cteInnerExpressions);
    }

    private ScalarSubqueryRewrite RewriteMaterializedUncorrelatedScalarSubquery(
        Node materializedBody,
        string materializedValueColumnName,
        string cteName,
        string keyColumnName,
        string valueColumnName,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        var materializedCteName = CreateScalarMaterializationCteName(cteName);
        cteInnerExpressions.Add(new CteInnerExpressionNode(materializedBody, materializedCteName));
        var fields = new[]
        {
            new FieldNode(new IntegerNode(1), 0, keyColumnName),
            new FieldNode(
                CreateScalarAggregate(new AccessColumnNode(materializedValueColumnName, materializedCteName, default)),
                1,
                valueColumnName)
        };

        var rewritten = new QueryNode(
            new SelectNode(fields),
            new Parser.ExpressionFromNode(new Parser.InMemoryTableFromNode(materializedCteName, materializedCteName)),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            default);

        return new ScalarSubqueryRewrite(
            rewritten,
            new EqualityNode(new IntegerNode(1), new AccessColumnNode(keyColumnName, cteName, default)));
    }

    private string CreateScalarMaterializationCteName(string cteName) =>
        CreateUniqueScalarMaterializationCteName(cteName);
}
