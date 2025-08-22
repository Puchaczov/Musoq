namespace Musoq.Parser.Tokens;

public class ByToken : Token
{
    public const string TokenText = "by";

    public ByToken(TextSpan span)
        : base(TokenText, TokenType.By, span)
    {
    }
}