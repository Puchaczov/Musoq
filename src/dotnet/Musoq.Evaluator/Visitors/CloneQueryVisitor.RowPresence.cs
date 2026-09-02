using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(RowPresenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new RowPresenceNode(Nodes.Pop(), node.IsPresent)
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }
}
