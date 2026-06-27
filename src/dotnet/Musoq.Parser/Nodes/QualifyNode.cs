namespace Musoq.Parser.Nodes;

public class QualifyNode(Node expression) : Node
{
    public Node Expression { get; } = expression;

    public override Type? ReturnType => null;

    public override string Id { get; } = $"{nameof(QualifyNode)}{expression.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"qualify {Expression.ToString()}";
    }
}
