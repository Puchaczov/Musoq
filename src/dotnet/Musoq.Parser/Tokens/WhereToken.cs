namespace Musoq.Parser.Tokens;

public class WhereToken(TextSpan span) : Token(TokenText, TokenType.Where, span)
{
    public const string TokenText = "where";
}
