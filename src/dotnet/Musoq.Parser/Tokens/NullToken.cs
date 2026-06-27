namespace Musoq.Parser.Tokens;

public class NullToken(TextSpan span) : Token(TokenText, TokenType.Null, span)
{
    public const string TokenText = "null";
}
