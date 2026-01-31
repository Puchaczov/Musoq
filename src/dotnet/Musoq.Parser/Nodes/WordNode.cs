using System;

namespace Musoq.Parser.Nodes;

public class WordNode : ConstantValueNode
{
    public WordNode(string value)
        : this(value, default)
    {
    }

    public WordNode(string value, TextSpan span)
    {
        Value = value;
        Id = $"{nameof(WordNode)}{value}{ReturnType.Name}";
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
