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
        var operand = InfixExpressionText.FormatPredicateOperand(Expression);
        return IsNegated ? $"{operand} is not null" : $"{operand} is null";
    }
}
