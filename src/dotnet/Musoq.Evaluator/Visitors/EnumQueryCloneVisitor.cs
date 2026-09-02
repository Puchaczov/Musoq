using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(EnumDeclarationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var members = new EnumMemberNode[node.Members.Count];
        for (var index = members.Length - 1; index >= 0; index--)
            members[index] = (EnumMemberNode)Nodes.Pop();

        Nodes.Push(new EnumDeclarationNode(
            node.Name,
            node.UnderlyingTypeName,
            node.IsFlags,
            members,
            node.NameSpan,
            node.UnderlyingTypeSpan,
            node.Span).WithFullSpan(node.FullSpan));
    }

    public override void Visit(EnumMemberNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new EnumMemberNode(
            node.Name,
            node.RawValue,
            node.LiteralText,
            node.NameSpan,
            node.ValueSpan,
            node.Span).WithFullSpan(node.FullSpan));
    }
}
