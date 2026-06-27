namespace Musoq.Parser.Nodes;

public class BinaryIntegerNode : ConstantValueNode
{
    public BinaryIntegerNode(string value)
        : this(value, default)
    {
    }

    public BinaryIntegerNode(string value, TextSpan span)
    {
        ArgumentNullException.ThrowIfNull(value);
        var binaryValue = value.StartsWith("0b", StringComparison.OrdinalIgnoreCase)
            ? value.Substring(2)
            : value;

        ObjValue = ParseBinaryValue(binaryValue, value);
        Id = $"{nameof(BinaryIntegerNode)}{value}{ReturnType.Name}";
        Span = span;
        FullSpan = span;
    }

    public BinaryIntegerNode(object objValue)
        : this(objValue, default)
    {
    }

    public BinaryIntegerNode(object objValue, TextSpan span)
    {
        ObjValue = objValue;
        Id = $"{nameof(BinaryIntegerNode)}{objValue}{ObjValue.GetType().Name}";
        Span = span;
        FullSpan = span;
    }

    public sealed override object ObjValue { get; }

    public sealed override Type ReturnType => ObjValue.GetType();

    public override string Id { get; }

    public override string ToString()
    {
        return ObjValue.ToString() ?? string.Empty;
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    private static long ParseBinaryValue(string binaryValue, string originalValue)
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
