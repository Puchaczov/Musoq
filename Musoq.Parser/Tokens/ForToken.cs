namespace Musoq.Parser.Tokens;

public class ForToken : Token
{
    public const string TokenText = "for";

    public ForToken(TextSpan span)
        : base(TokenText, TokenType.For, span)
    {
    }
}