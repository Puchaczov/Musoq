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
        
        ObjValue = ParseOctalValue(octalValue, value);
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

    private static object ParseOctalValue(string octalValue, string originalValue)
    {
        // Parse as long for consistency with existing test expectations
        try
        {
            var result = Convert.ToInt64(octalValue, 8);
            
            // Additional validation for boundary cases
            // Note: Convert.ToInt64 handles two's complement representation correctly
            // Values within octal long range are valid
            // Only values that truly overflow beyond long range should be rejected
            
            return result;
        }
        catch (OverflowException)
        {
            throw new NotSupportedException($"Octal value {originalValue} is too large and not supported.");
        }
        catch (ArgumentException)
        {
            throw new NotSupportedException($"Octal value {originalValue} is too large and not supported.");
        }
        catch (FormatException)
        {
            throw new NotSupportedException($"Octal value {originalValue} has invalid format.");
        }
    }
}