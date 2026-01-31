using System;

namespace Musoq.Parser.Nodes;

public class WhereNode : Node
{
    public WhereNode(Node expression)
        : this(expression, default)
    {
    }

    public WhereNode(Node expression, TextSpan span)
    {
        Expression = expression;
        Id = $"{nameof(WhereNode)}{expression.Id}";

        // If no explicit span, inherit from expression
        if (span.IsEmpty && expression?.HasSpan == true)
        {
            Span = expression.Span;
            FullSpan = expression.Span;
        }
        else
        {
            Span = span;
            FullSpan = span;
        }
    }

    public Node Expression { get; }

    public override Type ReturnType => Expression.ReturnType;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"where {Expression.ToString()}";
    }
}
