using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class RewriteFieldWithGroupMethodCall(FieldNode[] nodes)
    : RewriteFieldWithGroupMethodCallBase<FieldNode, FieldNode>(nodes)
{
    public override void Visit(FieldNode node)
    {
        base.Visit(node);
        Expression = Nodes.Pop() as FieldNode
                     ?? throw new InvalidOperationException("Expected a rewritten field node.");
    }

    protected override string ExtractOriginalExpression(FieldNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.Expression.ToString();
    }
}
