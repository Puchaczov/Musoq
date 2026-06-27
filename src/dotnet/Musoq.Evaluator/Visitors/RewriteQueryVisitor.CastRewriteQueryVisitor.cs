using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    public void Visit(CastNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new CastNode(Nodes.Pop(), node.TargetTypeName, node.ReturnType).WithSpan(node.Span));
    }
}
