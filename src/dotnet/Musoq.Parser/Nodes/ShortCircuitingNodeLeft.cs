using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class ShortCircuitingNodeLeft(Node expression, TokenType usedFor) : Node
{
    public TokenType UsedFor { get; } = usedFor;

    public Node Expression { get; } = expression;
    public override Type? ReturnType => Expression.ReturnType;

    public override string Id { get; } = $"{nameof(ShortCircuitingNodeLeft)}{expression.Id}";

    public override string ToString()
    {
        return Expression.ToString();
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}
