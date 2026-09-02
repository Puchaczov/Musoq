namespace Musoq.Parser.Nodes;

/// <summary>
///     Declares one explicitly-valued query-local enum member.
/// </summary>
public sealed class EnumMemberNode : Node
{
    public EnumMemberNode(
        string name,
        ulong rawValue,
        string literalText,
        TextSpan nameSpan,
        TextSpan valueSpan,
        TextSpan span)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(literalText);

        Name = name;
        RawValue = rawValue;
        LiteralText = literalText;
        NameSpan = nameSpan;
        ValueSpan = valueSpan;
        Span = span;
        FullSpan = span;
        Id = $"{nameof(EnumMemberNode)}{Name}{RawValue}";
    }

    public string Name { get; }

    /// <summary>
    ///     Gets the value normalized to the declared backing width. Signed values use
    ///     their two's-complement bit representation.
    /// </summary>
    public ulong RawValue { get; }

    public string LiteralText { get; }

    public TextSpan NameSpan { get; }

    public TextSpan ValueSpan { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Name} = {LiteralText}";
    }
}
