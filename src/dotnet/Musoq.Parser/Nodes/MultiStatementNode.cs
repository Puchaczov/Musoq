using System.Linq;

namespace Musoq.Parser.Nodes;

public class MultiStatementNode(Node[] nodes, Type? returnType) : Node
{
    public Node[] Nodes { get; } = nodes;

    public override Type? ReturnType { get; } = returnType;

    public override string Id { get; } = $"{nameof(MultiStatementNode)}{string.Concat(nodes.Select(node => node.Id))}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, Nodes.Select(f => f.ToString()));
    }
}
