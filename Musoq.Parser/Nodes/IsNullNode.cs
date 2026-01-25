using System;

namespace Musoq.Parser.Nodes;

public class IsNullNode : Node
{
    public IsNullNode(Node expression, bool isNegated)
    {
        IsNegated = isNegated;
        Expression = expression;
        Id = $"{nameof(IsNullNode)}{expression.Id}{isNegated}";
    }

    public Node Expression { get; }

    public bool IsNegated { get; }

    public override Type ReturnType => typeof(bool);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return IsNegated ? $"{Expression.ToString()} is not null" : $"{Expression.ToString()} is null";
    }
}
