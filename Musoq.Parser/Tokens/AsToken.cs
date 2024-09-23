namespace Musoq.Parser.Tokens;

public class AsToken : Token
{
    public const string TokenText = "as";

    public AsToken(TextSpan span)
        : base(TokenText, TokenType.As, span)
    {
    }
}