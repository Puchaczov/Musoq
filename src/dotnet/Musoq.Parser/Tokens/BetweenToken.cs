namespace Musoq.Parser.Tokens;

public class BetweenToken(TextSpan span) : Token(TokenText, TokenType.Between, span)
{
    public const string TokenText = "between";
}
