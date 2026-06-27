namespace Musoq.Parser.Tokens;

public class WindowToken(TextSpan span) : Token(TokenText, TokenType.Window, span)
{
    public const string TokenText = "window";
}
