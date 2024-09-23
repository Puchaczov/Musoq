namespace Musoq.Parser.Tokens;

public class LikeToken : Token
{
    public const string TokenText = "like";

    public LikeToken(TextSpan span)
        : base(TokenText, TokenType.Like, span)
    {
    }
}