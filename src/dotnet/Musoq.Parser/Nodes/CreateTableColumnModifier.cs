namespace Musoq.Parser.Nodes;

public sealed record CreateTableColumnModifier(string Key, string Value)
{
    public CreateTableColumnModifier(string key, string value, TextSpan span)
        : this(key, value)
    {
        Span = span;
    }

    public TextSpan Span { get; init; }
}
