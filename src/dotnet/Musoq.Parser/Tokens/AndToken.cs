namespace Musoq.Parser.Tokens;

public class AndToken(TextSpan span) : Token(TokenText, TokenType.And, span)
{
    public const string TokenText = "and";
}
