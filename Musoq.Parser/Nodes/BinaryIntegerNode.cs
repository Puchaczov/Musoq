using System;

namespace Musoq.Parser.Nodes;

public class BinaryIntegerNode : ConstantValueNode
{
    public BinaryIntegerNode(string value)
    {
        // Remove 0b prefix and parse as binary
        var binaryValue = value.StartsWith("0b", StringComparison.OrdinalIgnoreCase) 
            ? value.Substring(2) 
            : value;
        
        ObjValue = Convert.ToInt64(binaryValue, 2);
        Id = $"{nameof(BinaryIntegerNode)}{value}{ReturnType.Name}";
    }

    public BinaryIntegerNode(object objValue)
    {
        ObjValue = objValue;
        Id = $"{nameof(BinaryIntegerNode)}{objValue}{ObjValue.GetType().Name}";
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