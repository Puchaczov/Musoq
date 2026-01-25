namespace Musoq.Parser.Tokens;

public class NotToken : Token
{
    public const string TokenText = "not";

    public NotToken(TextSpan span)
        : base(TokenText, TokenType.Not, span)
    {
    }
}
