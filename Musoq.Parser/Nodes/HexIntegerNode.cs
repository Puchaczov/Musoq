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
        
        ObjValue = ParseHexValue(hexValue, value);
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

    private static object ParseHexValue(string hexValue, string originalValue)
    {
        // Parse as long for consistency with existing test expectations
        try
        {
            var result = Convert.ToInt64(hexValue, 16);
            
            // Additional validation for boundary cases
            // Note: Convert.ToInt64 handles two's complement representation correctly
            // Values like 0x8000000000000000 are valid (long.MinValue)
            // Only values that truly overflow beyond long range should be rejected
            
            return result;
        }
        catch (OverflowException)
        {
            throw new NotSupportedException($"Hexadecimal value {originalValue} is too large and not supported.");
        }
        catch (ArgumentException)
        {
            throw new NotSupportedException($"Hexadecimal value {originalValue} is too large and not supported.");
        }
        catch (FormatException)
        {
            throw new NotSupportedException($"Hexadecimal value {originalValue} has invalid format.");
        }
    }
}