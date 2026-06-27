namespace Musoq.Parser.Tokens;

public class TrueToken(TextSpan span) : Token(TokenText, TokenType.True, span)
{
    public const string TokenText = "true";
}
