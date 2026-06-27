namespace Musoq.Parser.Tokens;

public class InnerJoinToken(TextSpan span) : Token(TokenText, TokenType.InnerJoin, span)
{
    public const string TokenText = "inner join";
    public const string TokenTextShort = "join";
}
