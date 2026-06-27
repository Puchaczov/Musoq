namespace Musoq.Parser.Tokens;

public class StarToken(TextSpan span) : Token(TokenText, TokenType.Star, span)
{
    public const string TokenText = "*";
}
