namespace Musoq.Parser.Tokens;

public class EqualityToken(TextSpan span) : Token(TokenText, TokenType.Equality, span)
{
    public const string TokenText = "=";
}
