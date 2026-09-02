namespace Musoq.Parser.Nodes;

public class IntegerNode : ConstantValueNode
{
    public IntegerNode(string value, string abbreviation)
        : this(value, abbreviation, default)
    {
    }

    public IntegerNode(string value, string abbreviation, TextSpan span)
    {
        ArgumentNullException.ThrowIfNull(abbreviation);
        ObjValue = abbreviation.ToUpperInvariant() switch
        {
            "B" => sbyte.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            "UB" => byte.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            "S" => short.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            "US" => ushort.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            "I" => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            "UI" => uint.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            "L" => long.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            "UL" => ulong.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            _ => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)
        };

        Id = $"{nameof(IntegerNode)}{value}{ReturnType.Name}";
        Span = span;
        FullSpan = span;
    }

    public IntegerNode(object objValue)
        : this(objValue, default)
    {
    }

    public IntegerNode(object objValue, TextSpan span)
    {
        ObjValue = objValue;
        Id = $"{nameof(IntegerNode)}{objValue}{ObjValue.GetType().Name}";
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

}
