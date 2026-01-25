namespace Musoq.Parser.Tokens;

public class RightShiftToken : Token
{
    public const string TokenText = ">>";

    public RightShiftToken(TextSpan span)
        : base(TokenText, TokenType.RightShift, span)
    {
    }
}
