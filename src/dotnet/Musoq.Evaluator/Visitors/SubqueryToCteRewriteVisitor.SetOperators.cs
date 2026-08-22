using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    public override void Visit(UnionNode node)
    {
        RewriteSetOperator(node);
    }

    public override void Visit(UnionAllNode node)
    {
        RewriteSetOperator(node);
    }

    public override void Visit(ExceptNode node)
    {
        RewriteSetOperator(node);
    }

    public override void Visit(IntersectNode node)
    {
        RewriteSetOperator(node);
    }

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
                orderBy, skip, take),
            UnionAllNode => new UnionAllNode(
                node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                orderBy, skip, take),
            ExceptNode => new ExceptNode(
                node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                orderBy, skip, take),
            IntersectNode => new IntersectNode(
                node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                orderBy, skip, take),
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
}
