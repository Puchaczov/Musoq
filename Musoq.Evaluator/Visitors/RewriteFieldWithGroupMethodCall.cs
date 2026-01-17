using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class RewriteFieldWithGroupMethodCall(FieldNode[] nodes)
    : RewriteFieldWithGroupMethodCallBase<FieldNode, FieldNode>(nodes)
{
    public override void Visit(FieldNode node)
    {
        base.Visit(node);
        Expression = Nodes.Pop() as FieldNode;
    }

    protected override string ExtractOriginalExpression(FieldNode node)
    {
        return node.Expression.ToString();
    }
}