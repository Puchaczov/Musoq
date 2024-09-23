namespace Musoq.Parser.Tokens;

public class NotRLikeToken : Token
{
    public const string TokenText = "not rlike";

    public NotRLikeToken(TextSpan span)
        : base(TokenText, TokenType.NotRLike, span)
    {
    }
}