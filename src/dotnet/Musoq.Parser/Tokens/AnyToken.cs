namespace Musoq.Parser.Tokens;

public class AnyToken(TextSpan span) : Token(TokenText, TokenType.Any, span)
{
    public const string TokenText = "any";
}
