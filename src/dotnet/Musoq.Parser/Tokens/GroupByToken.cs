namespace Musoq.Parser.Tokens;

public class GroupByToken(TextSpan span) : Token(TokenText, TokenType.GroupBy, span)
{
    public const string TokenText = "group by";
}
