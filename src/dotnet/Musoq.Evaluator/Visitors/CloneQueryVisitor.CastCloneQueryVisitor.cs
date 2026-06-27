using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(CastNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var expression = SafePop(Nodes, nameof(Visit) + nameof(CastNode));
        Nodes.Push(new CastNode(expression, node.TargetTypeName, node.ReturnType).WithSpan(node.Span));
    }
}
