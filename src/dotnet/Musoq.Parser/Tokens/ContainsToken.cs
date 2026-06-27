namespace Musoq.Parser.Tokens;

public class ContainsToken(TextSpan span) : Token(TokenText, TokenType.Contains, span)
{
    public const string TokenText = "contains";
}
