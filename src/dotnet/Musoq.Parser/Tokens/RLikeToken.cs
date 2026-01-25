namespace Musoq.Parser.Tokens;

public class RLikeToken : Token
{
    public const string TokenText = "rlike";

    public RLikeToken(TextSpan span)
        : base(TokenText, TokenType.RLike, span)
    {
    }
}
