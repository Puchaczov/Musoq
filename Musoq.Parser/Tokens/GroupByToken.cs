namespace Musoq.Parser.Tokens;

public class GroupByToken : Token
{
    public static string TokenText = "group by";

    public GroupByToken(TextSpan span)
        : base(TokenText, TokenType.GroupBy, span)
    {
    }
}