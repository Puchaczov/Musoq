using System;

namespace Musoq.Parser.Nodes;

public class StatementNode : Node
{
    public StatementNode(Node node)
    {
        ReturnType = typeof(void);
        Node = node;
        Id = null;
    }

    public Node Node { get; }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Node.ToString();
    }
}
