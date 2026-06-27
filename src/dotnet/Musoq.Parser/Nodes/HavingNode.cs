namespace Musoq.Parser.Nodes;

public class HavingNode(Node expression) : Node
{
    public Node Expression { get; } = expression;

    public override Type? ReturnType => null;

    public override string Id { get; } = $"{nameof(HavingNode)}{expression.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"having {Expression.ToString()}";
    }
}
