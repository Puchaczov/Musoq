namespace Musoq.Parser.Tokens;

public class RLikeToken(TextSpan span) : Token(TokenText, TokenType.RLike, span)
{
    public const string TokenText = "rlike";
}
