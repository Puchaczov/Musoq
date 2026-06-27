namespace Musoq.Parser.Tokens;

public class NotToken(TextSpan span) : Token(TokenText, TokenType.Not, span)
{
    public const string TokenText = "not";
}
