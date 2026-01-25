namespace Musoq.Parser.Tokens;

public class LeftShiftToken : Token
{
    public const string TokenText = "<<";

    public LeftShiftToken(TextSpan span)
        : base(TokenText, TokenType.LeftShift, span)
    {
    }
}
