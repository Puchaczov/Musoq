using System;

namespace Musoq.Parser.Nodes;

public class ElseNode : UnaryNode
{
    public ElseNode(Node expression)
        : base(expression)
    {
        Id = $"{nameof(ElseNode)}{Expression.Id}";
    }

    public override Type ReturnType => Expression.ReturnType;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"else {Expression.ToString()}";
    }
}
