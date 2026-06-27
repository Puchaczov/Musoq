namespace Musoq.Parser.Tokens;

public class IntegerToken(string value, TextSpan span, string abbreviation) : Token(value, TokenType.Integer, span)
{
    public const string TokenText = "numeric";

    public string Abbreviation { get; } = abbreviation;
}
