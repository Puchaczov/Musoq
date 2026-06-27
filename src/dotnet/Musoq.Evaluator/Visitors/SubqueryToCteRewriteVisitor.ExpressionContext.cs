using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private static SubqueryRewriteContext RewriteExpressionContext<TJoin>(
        SubqueryRewriteContext context,
        ISubqueryExpressionContextRewriter<TJoin> rewriter,
        Func<FromNode, TJoin, FromNode> appendJoin)
    {
        var select = RewriteExpressionContext(context.Select, rewriter);
        var from = RewriteExpressionContext(context.From, rewriter, appendJoin);
        var whereExpression = RewriteExpressionContext(context.WhereExpression, rewriter);
        var groupBy = RewriteExpressionContext(context.GroupBy, rewriter);
        var window = RewriteExpressionContext(context.Window, rewriter);
        var orderBy = RewriteExpressionContext(context.OrderBy, rewriter);
        var qualify = context.Qualify != null
            ? new QualifyNode(rewriter.Rewrite(context.Qualify.Expression))
            : null;

        foreach (var join in rewriter.Joins)
            from = appendJoin(from, join);

        return new SubqueryRewriteContext(select, from, whereExpression, groupBy, orderBy, window, qualify);
    }

    private static SelectNode RewriteExpressionContext<TJoin>(
        SelectNode select,
        ISubqueryExpressionContextRewriter<TJoin> rewriter)
    {
        var fields = new FieldNode[select.Fields.Length];

        for (var i = 0; i < select.Fields.Length; i++)
        {
            var field = select.Fields[i];
            fields[i] = new FieldNode(
                rewriter.Rewrite(field.Expression),
                field.FieldOrder,
                field.FieldName,
                field.HasExplicitFieldName,
                field.Span);
        }

        return new SelectNode(fields, select.IsDistinct, select.Span);
    }

    private static GroupByNode? RewriteExpressionContext<TJoin>(
        GroupByNode? groupBy,
        ISubqueryExpressionContextRewriter<TJoin> rewriter)
    {
        if (groupBy == null)
            return null;

        var fields = new FieldNode[groupBy.Fields.Length];
        for (var i = 0; i < groupBy.Fields.Length; i++)
        {
            var field = groupBy.Fields[i];
            fields[i] = new FieldNode(
                rewriter.Rewrite(field.Expression),
                field.FieldOrder,
                field.FieldName,
                field.HasExplicitFieldName,
                field.Span);
        }

        var having = groupBy.Having != null
            ? new HavingNode(rewriter.Rewrite(groupBy.Having.Expression))
            : null;

        return new GroupByNode(fields, having, groupBy.IsAll, groupBy.Span);
    }

    private static OrderByNode? RewriteExpressionContext<TJoin>(
        OrderByNode? orderBy,
        ISubqueryExpressionContextRewriter<TJoin> rewriter)
    {
        if (orderBy == null)
            return null;

        var fields = new FieldOrderedNode[orderBy.Fields.Length];
        for (var i = 0; i < orderBy.Fields.Length; i++)
        {
            var field = orderBy.Fields[i];
            fields[i] = new FieldOrderedNode(
                rewriter.Rewrite(field.Expression),
                field.FieldOrder,
                field.FieldName,
                field.HasExplicitFieldName,
                field.Order,
                field.NullOrdering);
        }

        return new OrderByNode(fields, orderBy.Span);
    }

    private static WindowNode? RewriteExpressionContext<TJoin>(
        WindowNode? window,
        ISubqueryExpressionContextRewriter<TJoin> rewriter)
    {
        if (window == null)
            return null;

        var definitions = new WindowDefinitionNode[window.Definitions.Length];
        for (var i = 0; i < window.Definitions.Length; i++)
        {
            var definition = window.Definitions[i];
            definitions[i] = new WindowDefinitionNode(
                definition.Name,
                RewriteExpressionContext(definition.Specification, rewriter)
                ?? throw new InvalidOperationException("Window definition is missing a specification."));
        }

        return new WindowNode(definitions);
    }

    private static WindowSpecificationNode? RewriteExpressionContext<TJoin>(
        WindowSpecificationNode? specification,
        ISubqueryExpressionContextRewriter<TJoin> rewriter)
    {
        if (specification == null)
            return null;

        var partitionFields = new FieldNode[specification.PartitionFields.Length];
        for (var i = 0; i < specification.PartitionFields.Length; i++)
        {
            var field = specification.PartitionFields[i];
            partitionFields[i] = new FieldNode(
                rewriter.Rewrite(field.Expression),
                field.FieldOrder,
                field.FieldName,
                field.HasExplicitFieldName,
                field.Span);
        }

        var orderByFields = new FieldOrderedNode[specification.OrderByFields.Length];
        for (var i = 0; i < specification.OrderByFields.Length; i++)
        {
            var field = specification.OrderByFields[i];
            orderByFields[i] = new FieldOrderedNode(
                rewriter.Rewrite(field.Expression),
                field.FieldOrder,
                field.FieldName,
                field.HasExplicitFieldName,
                field.Order,
                field.NullOrdering);
        }

        return new WindowSpecificationNode(partitionFields, orderByFields, specification.Frame);
    }

    private static FromNode RewriteExpressionContext<TJoin>(
        FromNode from,
        ISubqueryExpressionContextRewriter<TJoin> rewriter,
        Func<FromNode, TJoin, FromNode> appendJoin)
    {
        return from switch
        {
            Parser.JoinFromNode join => RewriteExpressionContext(join, rewriter, appendJoin),
            Parser.ExpressionFromNode { Expression: Parser.JoinNode joinNode } =>
                new Parser.ExpressionFromNode(new Parser.JoinNode((Parser.JoinFromNode)RewriteExpressionContext(joinNode.Join, rewriter, appendJoin))),
            Parser.ExpressionFromNode expressionFrom =>
                new Parser.ExpressionFromNode(RewriteExpressionContext(expressionFrom.Expression, rewriter, appendJoin)),
            _ => from
        };
    }

    private static Parser.JoinFromNode RewriteExpressionContext<TJoin>(
        Parser.JoinFromNode join,
        ISubqueryExpressionContextRewriter<TJoin> rewriter,
        Func<FromNode, TJoin, FromNode> appendJoin)
    {
        var source = RewriteExpressionContext(join.Source, rewriter, appendJoin);
        var with = RewriteExpressionContext(join.With, rewriter, appendJoin);
        var joinMark = rewriter.JoinCount;
        var expression = rewriter.Rewrite(join.Expression);
        var localJoins = rewriter.TakeJoinsFrom(joinMark);

        foreach (var localJoin in localJoins)
            source = appendJoin(source, localJoin);

        return new Parser.JoinFromNode(source, with, expression, join.JoinType);
    }

    private static Node? RewriteExpressionContext<TJoin>(
        Node? expression,
        ISubqueryExpressionContextRewriter<TJoin> rewriter)
    {
        return expression == null ? null : rewriter.Rewrite(expression);
    }

    private sealed record SubqueryRewriteContext(
        SelectNode Select,
        FromNode From,
        Node? WhereExpression,
        GroupByNode? GroupBy,
        OrderByNode? OrderBy,
        WindowNode? Window,
        QualifyNode? Qualify);

    private interface ISubqueryExpressionContextRewriter<TJoin>
    {
        IReadOnlyList<TJoin> Joins { get; }

        int JoinCount { get; }

        TJoin[] TakeJoinsFrom(int index);

        Node Rewrite(Node expression);
    }
}
