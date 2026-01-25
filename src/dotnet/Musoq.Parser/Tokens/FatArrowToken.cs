namespace Musoq.Parser.Tokens;

public class FatArrowToken : Token
{
    public const string TokenText = "=>";

    public FatArrowToken(TextSpan span)
        : base(TokenText, TokenType.FatArrow, span)
    {
    }
}
