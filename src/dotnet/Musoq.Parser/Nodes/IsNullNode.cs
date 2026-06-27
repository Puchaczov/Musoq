namespace Musoq.Parser.Nodes;

public class IsNullNode(Node expression, bool isNegated) : Node
{
    public Node Expression { get; } = expression;

    public bool IsNegated { get; } = isNegated;

    public override Type ReturnType => typeof(bool);

    public override string Id { get; } = $"{nameof(IsNullNode)}{expression.Id}{isNegated}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return IsNegated ? $"{Expression.ToString()} is not null" : $"{Expression.ToString()} is null";
    }
}
