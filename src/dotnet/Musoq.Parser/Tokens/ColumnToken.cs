namespace Musoq.Parser.Tokens;

public class ColumnToken : Token
{
    private readonly bool _hasColumnMarkers;

    public ColumnToken(string value, TextSpan span) : base(ReplaceLeadingAndTrailingColumnMarkers(value),
        TokenType.Identifier, span)
    {
        if (value.StartsWith("[") && value.EndsWith("]"))
            _hasColumnMarkers = true;
    }

    public override string ToString()
    {
        if (_hasColumnMarkers)
            return $"[{Value}]";

        return Value;
    }

    private static string ReplaceLeadingAndTrailingColumnMarkers(string value)
    {
        if (value.StartsWith("[") && value.EndsWith("]"))
            return value.Substring(1, value.Length - 2);

        return value;
    }
}
