using System;

namespace Musoq.Parser.Nodes;

public class HexIntegerNode : ConstantValueNode
{
    public HexIntegerNode(string value)
    {
        // Remove 0x prefix and parse as hex
        var hexValue = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) 
            ? value.Substring(2) 
            : value;
        
        ObjValue = Convert.ToInt64(hexValue, 16);
        Id = $"{nameof(HexIntegerNode)}{value}{ReturnType.Name}";
    }

    public HexIntegerNode(object objValue)
    {
        ObjValue = objValue;
        Id = $"{nameof(HexIntegerNode)}{objValue}{ObjValue.GetType().Name}";
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
}