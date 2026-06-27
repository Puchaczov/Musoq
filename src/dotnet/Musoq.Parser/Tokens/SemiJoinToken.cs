namespace Musoq.Parser.Tokens;

public class SemiJoinToken(TextSpan span) : Token(TokenText, TokenType.SemiJoin, span)
{
    public const string TokenText = "semi join";
}