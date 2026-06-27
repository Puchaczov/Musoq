namespace Musoq.Parser.Tokens;

public class FatArrowToken(TextSpan span) : Token(TokenText, TokenType.FatArrow, span)
{
    public const string TokenText = "=>";
}
