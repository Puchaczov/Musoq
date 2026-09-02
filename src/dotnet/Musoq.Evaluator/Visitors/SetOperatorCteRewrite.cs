using System.Collections.Generic;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private void RewriteSetOperator(SetOperatorNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        var cteDefinitions = new List<CteInnerExpressionNode>();
        left = UnwrapSetOperandCtes(left, cteDefinitions);
        right = UnwrapSetOperandCtes(right, cteDefinitions);

        SetOperatorNode rewritten = node switch
        {
            UnionNode => new UnionNode(
                node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                orderBy, skip, take)
            {
                KeySpans = node.KeySpans
            },
            UnionAllNode => new UnionAllNode(
                node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                orderBy, skip, take)
            {
                KeySpans = node.KeySpans
            },
            ExceptNode => new ExceptNode(
                node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                orderBy, skip, take)
            {
                KeySpans = node.KeySpans
            },
            IntersectNode => new IntersectNode(
                node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                orderBy, skip, take)
            {
                KeySpans = node.KeySpans
            },
            _ => throw new NotSupportedException($"Set operator '{node.GetType().Name}' is not supported.")
        };

        Nodes.Push(cteDefinitions.Count == 0
            ? rewritten
            : new CteExpressionNode(cteDefinitions.ToArray(), rewritten));
    }

    private static Node UnwrapSetOperandCtes(Node operand, List<CteInnerExpressionNode> definitions)
    {
        while (operand is CteExpressionNode cteExpression)
        {
            definitions.AddRange(cteExpression.InnerExpression);
            operand = cteExpression.OuterExpression;
        }

        return operand;
    }

    private static SetOperatorNode RecreateSetOperator(SetOperatorNode node, Node left, Node right)
    {
        return node switch
        {
            UnionNode => new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                node.ResultOrderBy, node.ResultSkip, node.ResultTake)
            {
                KeySpans = node.KeySpans
            },
            UnionAllNode => new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                node.ResultOrderBy, node.ResultSkip, node.ResultTake)
            {
                KeySpans = node.KeySpans
            },
            ExceptNode => new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                node.ResultOrderBy, node.ResultSkip, node.ResultTake)
            {
                KeySpans = node.KeySpans
            },
            IntersectNode => new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                node.ResultOrderBy, node.ResultSkip, node.ResultTake)
            {
                KeySpans = node.KeySpans
            },
            _ => throw SubqueryDiagnosticFactory.InvalidSubquery(
                "EXISTS subquery projection",
                $"Unsupported set operator {node.GetType().Name} in EXISTS subquery.",
                node)
        };
    }
}
