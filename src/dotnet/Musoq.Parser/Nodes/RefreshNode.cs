using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class RefreshNode : Node
{
    public RefreshNode(AccessMethodNode[] nodes)
    {
        Nodes = nodes;
        var nodesId = nodes.Length == 0 ? string.Empty : string.Concat(nodes.Select(f => f.Id));
        Id = $"{nameof(RefreshNode)}{nodesId}";
    }

    public AccessMethodNode[] Nodes { get; }

    public override Type ReturnType => null;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var methods = Nodes.Length == 0
            ? string.Empty
            : string.Join(", ", Nodes.Select(f => f.ToString()));

        return $"refresh ({methods})";
    }
}
