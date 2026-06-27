namespace Musoq.Parser.Tokens;

public class GreaterToken(TextSpan span) : Token(TokenText, TokenType.Greater, span)
{
    public const string TokenText = ">";
}
