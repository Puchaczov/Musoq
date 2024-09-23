namespace Musoq.Parser.Tokens;

public class InnerJoinToken : Token
{
    public const string TokenText = "inner join";
    public const string TokenTextShort = "join";

    public InnerJoinToken(TextSpan span)
        : base(TokenText, TokenType.InnerJoin, span)
    {
    }
}