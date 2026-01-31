using System;

namespace Musoq.Parser.Nodes;

public class StringNode : ConstantValueNode
{
    public StringNode(string value)
        : this(value, default)
    {
    }

    public StringNode(string value, TextSpan span)
    {
        Value = value;
        Id = $"{nameof(StringNode)}{ReturnType.Name}{value}";
        Span = span;
        FullSpan = span;
    }

    public string Value { get; }

    public override object ObjValue => Value;

    public override Type ReturnType => typeof(string);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"'{Value}'";
    }
}
