using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;
public partial class SubqueryToCteRewriteVisitor
{
    private static QueryNode GetLeftmostQuery(Node node)
    {
        var current = node;

        while (current is CteExpressionNode cteExpression)
            current = cteExpression.OuterExpression;

        while (current is SingleSetNode singleSet)
            current = singleSet.Query;

        while (current is SetOperatorNode setOp)
        {
            current = setOp.Left;
            while (current is CteExpressionNode cteExpression)
                current = cteExpression.OuterExpression;
            while (current is SingleSetNode singleSet)
                current = singleSet.Query;
        }

        return (QueryNode)current;
    }

    private static string GetSubqueryOutputColumnName(FieldNode field)
    {
        if (field.HasExplicitFieldName)
            return field.FieldName;

        return field.Expression switch
        {
            AccessColumnNode accessColumn => accessColumn.Name,
            DotNode { Root: IdentifierNode, Expression: IdentifierNode column } => column.Name,
            _ => field.FieldName
        };
    }

    private static bool ShouldRenameSubqueryOutput(FieldNode field)
    {
        return field is { HasExplicitFieldName: false, Expression: DotNode or AccessColumnNode { Alias: not null and not "" } };
    }

    private static QueryNode RenameSelectColumn(QueryNode query, string newAlias)
    {
        var originalField = query.Select.Fields[0];
        var renamedField = new FieldNode(originalField.Expression, originalField.FieldOrder, newAlias);
        var newFields = new FieldNode[query.Select.Fields.Length];
        newFields[0] = renamedField;

        for (var i = 1; i < query.Select.Fields.Length; i++)
            newFields[i] = query.Select.Fields[i];

        var newSelect = new SelectNode(newFields, query.Select.IsDistinct);

        return new QueryNode(
            newSelect,
            query.From,
            query.Where,
            query.GroupBy,
            query.OrderBy,
            query.Skip,
            query.Take,
            query.Window,
            query.Qualify,
            default);
    }
}
