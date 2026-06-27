namespace Musoq.Parser.Nodes;

public class StatementNode(Node node) : Node
{
    public Node Node { get; } = node;

    public override Type ReturnType { get; } = typeof(void);

    public override string Id { get; } = $"{nameof(StatementNode)}{node.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Node.ToString();
    }
}
