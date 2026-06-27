namespace Musoq.Parser.Tokens;

public class NotRLikeToken(TextSpan span) : Token(TokenText, TokenType.NotRLike, span)
{
    public const string TokenText = "not rlike";
}
