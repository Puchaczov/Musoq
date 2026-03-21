namespace Musoq.Parser.Tokens;

public class WindowToken : Token
{
    public const string TokenText = "window";

    public WindowToken(TextSpan span)
        : base(TokenText, TokenType.Window, span)
    {
    }
}
