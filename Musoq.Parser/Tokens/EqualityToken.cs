namespace Musoq.Parser.Tokens;

public class EqualityToken : Token
{
    public const string TokenText = "=";

    public EqualityToken(TextSpan span)
        : base(TokenText, TokenType.Equality, span)
    {
    }
}