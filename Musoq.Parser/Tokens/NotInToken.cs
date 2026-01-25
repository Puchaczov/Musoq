namespace Musoq.Parser.Tokens;

public class NotInToken : Token
{
    public const string TokenText = "not in";

    public NotInToken(TextSpan span)
        : base(TokenText, TokenType.NotIn, span)
    {
    }
}
