namespace Musoq.Parser.Nodes;

public class RootNode(Node node) : UnaryNode(node)
{
    public override Type? ReturnType => null;

    public override string Id { get; } = $"{nameof(RootNode)}{node.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Expression.ToString();
    }
}
