using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class RewriteFieldOrderedWithGroupMethodCall(FieldNode[] nodes) : RewriteFieldWithGroupMethodCallBase<FieldOrderedNode, FieldNode>(nodes)
{
    public override void Visit(FieldOrderedNode node)
    {
        base.Visit(node);
        Expression = Nodes.Pop() as FieldOrderedNode;
    }

    protected override string ExtractOriginalExpression(FieldNode node)
    {
        return node.FieldName;
    }
}