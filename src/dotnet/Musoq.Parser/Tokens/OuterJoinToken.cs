namespace Musoq.Parser.Tokens;

public class OuterJoinToken(OuterJoinType type, TextSpan span) : Token(TokenText, TokenType.OuterJoin, span)
{
    public const string TokenText = "outer join";

    public OuterJoinType Type { get; } = type;
}
