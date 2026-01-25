using System;

namespace Musoq.Parser.Nodes;

public class SkipNode : UnaryNode
{
    public SkipNode(IntegerNode expression) : base(expression)
    {
        Id = $"{nameof(SkipNode)}{ReturnType.Name}{Expression.Id}";
        Value = Convert.ToInt64(expression.ObjValue);
    }

    public long Value { get; }

    public override Type ReturnType => typeof(long);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"skip {Expression.ToString()}";
    }
}
