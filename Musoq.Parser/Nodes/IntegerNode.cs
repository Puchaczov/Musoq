using System;

namespace Musoq.Parser.Nodes;

public class IntegerNode : ConstantValueNode
{
    public IntegerNode(string value, string abbreviation)
    {
        ObjValue = abbreviation.ToLowerInvariant() switch
        {
            "b" => sbyte.Parse(value),
            "ub" => byte.Parse(value),
            "s" => short.Parse(value),
            "us" => ushort.Parse(value),
            "i" => int.Parse(value),
            "ui" => uint.Parse(value),
            "l" => long.Parse(value),
            "ul" => ulong.Parse(value),
            _ => Parse(value)
        };

        Id = $"{nameof(IntegerNode)}{value}{ReturnType.Name}";
    }

    public IntegerNode(object objValue)
    {
        ObjValue = objValue;
        Id = $"{nameof(IntegerNode)}{objValue}{ObjValue.GetType().Name}";
    }

    public sealed override object ObjValue { get; }

    public sealed override Type ReturnType => ObjValue.GetType();

    public override string Id { get; }

    public override string ToString()
    {
        return ObjValue.ToString();
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    private static object Parse(string value)
    {
        if (int.TryParse(value, out var value1))
        {
            return value1;
        }

        if (long.TryParse(value, out var value2))
        {
            return value2;
        }
            
        throw new NotSupportedException($"Integer value {value} is not supported.");
    }
}