using System;
using System.Globalization;

namespace Musoq.Parser.Nodes;

public class DecimalNode : ConstantValueNode
{
    public DecimalNode(string value)
        : this(value, default)
    {
    }

    public DecimalNode(string value, TextSpan span)
    {
        if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            throw new NotSupportedException($"The value '{value}' cannot be converted to decimal.");

        Value = result;
        Id = $"{nameof(DecimalNode)}{value}{ReturnType.Name}";
        Span = span;
        FullSpan = span;
    }

    public DecimalNode(decimal value)
        : this(value, default)
    {
    }

    public DecimalNode(decimal value, TextSpan span)
    {
        Value = value;
        Id = $"{nameof(DecimalNode)}{value}{ReturnType.Name}";
        Span = span;
        FullSpan = span;
    }

    public decimal Value { get; }

    public override object ObjValue => Value;

    public override Type ReturnType => typeof(decimal);

    public override string Id { get; }

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}
