namespace Musoq.Parser.Tokens;

public class PresentToken(TextSpan span) : Token(TokenText, TokenType.Present, span)
{
    public const string TokenText = "present";
}
