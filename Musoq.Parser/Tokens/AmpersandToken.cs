namespace Musoq.Parser.Tokens;

public class AmpersandToken : Token
{
    public const string TokenText = "&";

    public AmpersandToken(TextSpan span)
        : base(TokenText, TokenType.Ampersand, span)
    {
    }
}
