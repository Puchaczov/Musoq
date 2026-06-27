namespace Musoq.Parser.Tokens;

public class PivotToken(TextSpan span) : Token(TokenText, TokenType.Pivot, span)
{
    public const string TokenText = "pivot";
}
