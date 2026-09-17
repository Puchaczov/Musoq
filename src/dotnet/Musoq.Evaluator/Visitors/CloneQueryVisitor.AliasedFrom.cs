using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
namespace Musoq.Evaluator.Visitors;
public partial class CloneQueryVisitor
{
    public override void Visit(AliasedFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var cloned = new Parser.AliasedFromNode(node.Identifier, (ArgsListNode)Nodes.Pop(), node.Alias, node.InSourcePosition, node.TypeParameter).CopySpansFrom(node);
        if (node.MethodSpan is { } methodSpan) cloned.WithMethodSpan(methodSpan);
        Nodes.Push(cloned);
    }
}
