using System;

namespace Musoq.Parser.Nodes;

public class OctalIntegerNode : ConstantValueNode
{
    public OctalIntegerNode(string value)
    {
        // Remove 0o prefix and parse as octal
        var octalValue = value.StartsWith("0o", StringComparison.OrdinalIgnoreCase) 
            ? value.Substring(2) 
            : value;
        
        ObjValue = Convert.ToInt64(octalValue, 8);
        Id = $"{nameof(OctalIntegerNode)}{value}{ReturnType.Name}";
    }

    public OctalIntegerNode(object objValue)
    {
        ObjValue = objValue;
        Id = $"{nameof(OctalIntegerNode)}{objValue}{ObjValue.GetType().Name}";
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