namespace Musoq.Parser.Tokens;

public class RightShiftToken(TextSpan span) : Token(TokenText, TokenType.RightShift, span)
{
    public const string TokenText = ">>";
}
