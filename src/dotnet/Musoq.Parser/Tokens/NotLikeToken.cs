namespace Musoq.Parser.Tokens;

public class NotLikeToken : Token
{
    public const string TokenText = "not like";

    public NotLikeToken(TextSpan span)
        : base(TokenText, TokenType.NotLike, span)
    {
    }
}
