using System;

namespace Musoq.Parser.Nodes;

public class BinaryIntegerNode : ConstantValueNode
{
    public BinaryIntegerNode(string value)
    {
        var binaryValue = value.StartsWith("0b", StringComparison.OrdinalIgnoreCase)
            ? value.Substring(2)
            : value;

        ObjValue = ParseBinaryValue(binaryValue, value);
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

    private static object ParseBinaryValue(string binaryValue, string originalValue)
    {
        try
        {
            var result = Convert.ToInt64(binaryValue, 2);

            return result;
        }
        catch (OverflowException)
        {
            throw new NotSupportedException($"Binary value {originalValue} is too large and not supported.");
        }
        catch (ArgumentException)
        {
            throw new NotSupportedException($"Binary value {originalValue} is too large and not supported.");
        }
        catch (FormatException)
        {
            throw new NotSupportedException($"Binary value {originalValue} has invalid format.");
        }
    }
}
