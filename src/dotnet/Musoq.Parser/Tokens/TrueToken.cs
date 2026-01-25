namespace Musoq.Parser.Tokens;

public class TrueToken : Token
{
    public const string TokenText = "true";

    public TrueToken(TextSpan span)
        : base(TokenText, TokenType.True, span)
    {
    }
}
