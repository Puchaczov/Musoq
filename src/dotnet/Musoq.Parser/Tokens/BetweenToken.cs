namespace Musoq.Parser.Tokens;

public class BetweenToken : Token
{
    public const string TokenText = "between";

    public BetweenToken(TextSpan span)
        : base(TokenText, TokenType.Between, span)
    {
    }
}
