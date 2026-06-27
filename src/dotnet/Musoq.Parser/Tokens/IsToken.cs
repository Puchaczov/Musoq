namespace Musoq.Parser.Tokens;

public class IsToken(TextSpan span) : Token(TokenText, TokenType.Is, span)
{
    public const string TokenText = "is";
}
