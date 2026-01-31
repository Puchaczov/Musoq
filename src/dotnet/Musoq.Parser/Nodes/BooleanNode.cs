using System;

namespace Musoq.Parser.Nodes;

public class BooleanNode : ConstantValueNode
{
    public BooleanNode(bool value)
        : this(value, default)
    {
    }

    public BooleanNode(bool value, TextSpan span)
    {
        Value = value;
        Id = $"{nameof(BooleanNode)}{value}{ReturnType.Name}";
        Span = span;
        FullSpan = span;
    }

    public bool Value { get; }

    public override object ObjValue => Value;

    public override Type ReturnType => typeof(bool);

    public override string Id { get; }

    public override string ToString()
    {
        return Value ? "true" : "false";
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}
