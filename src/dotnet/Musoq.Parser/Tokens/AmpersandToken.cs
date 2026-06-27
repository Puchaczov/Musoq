namespace Musoq.Parser.Tokens;

public class AmpersandToken(TextSpan span) : Token(TokenText, TokenType.Ampersand, span)
{
    public const string TokenText = "&";
}
