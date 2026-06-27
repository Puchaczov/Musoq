namespace Musoq.Parser.Tokens;

public class LeftShiftToken(TextSpan span) : Token(TokenText, TokenType.LeftShift, span)
{
    public const string TokenText = "<<";
}
