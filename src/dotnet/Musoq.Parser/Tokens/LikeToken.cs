namespace Musoq.Parser.Tokens;

public class LikeToken(TextSpan span) : Token(TokenText, TokenType.Like, span)
{
    public const string TokenText = "like";
}
