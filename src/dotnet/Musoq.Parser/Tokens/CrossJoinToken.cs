namespace Musoq.Parser.Tokens;

public class CrossJoinToken(TextSpan span) : Token(TokenText, TokenType.CrossJoin, span)
{
    public const string TokenText = "cross join";
}