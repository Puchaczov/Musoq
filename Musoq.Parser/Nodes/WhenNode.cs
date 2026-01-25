using System;

namespace Musoq.Parser.Nodes;

public class WhenNode : UnaryNode
{
    public WhenNode(Node expression)
        : base(expression)
    {
        Id = $"{nameof(WhenNode)}{ReturnType.Name}{Expression.Id}";
    }

    public override Type ReturnType => typeof(bool);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"when {Expression.ToString()}";
    }
}
