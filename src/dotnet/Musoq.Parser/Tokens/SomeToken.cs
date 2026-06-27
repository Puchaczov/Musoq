namespace Musoq.Parser.Tokens;

public class SomeToken(TextSpan span) : Token(TokenText, TokenType.Some, span)
{
    public const string TokenText = "some";
}
