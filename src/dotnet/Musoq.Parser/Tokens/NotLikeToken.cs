namespace Musoq.Parser.Tokens;

public class NotLikeToken(TextSpan span) : Token(TokenText, TokenType.NotLike, span)
{
    public const string TokenText = "not like";
}
