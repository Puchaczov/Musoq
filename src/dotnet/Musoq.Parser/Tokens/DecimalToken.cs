namespace Musoq.Parser.Tokens;

public class DecimalToken(string value, TextSpan span) : Token(value, TokenType.Decimal, span)
{
    public const string TokenText = "numeric";
}
