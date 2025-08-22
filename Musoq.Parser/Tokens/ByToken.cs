namespace Musoq.Parser.Tokens;

public class ByToken : Token
{
    public const string TokenText = "BY";

    public ByToken(TextSpan span)
        : base(TokenText, TokenType.By, span)
    {
    }
}