namespace Musoq.Parser.Tokens;

public class OrderByToken(TextSpan span) : Token(TokenText, TokenType.OrderBy, span)
{
    public const string TokenText = "order by";
}
