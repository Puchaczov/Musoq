using System;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class ShortCircuitingNodeRight : Node
{
    public ShortCircuitingNodeRight(Node expression, TokenType usedFor)
    {
        Expression = expression;
        UsedFor = usedFor;
        Id = $"{nameof(ShortCircuitingNodeRight)}{expression.Id}";
    }


    public TokenType UsedFor { get; }
    public Node Expression { get; }
    public override Type ReturnType => Expression.ReturnType;

    public override string Id { get; }

    public override string ToString()
    {
        return Expression.ToString();
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}