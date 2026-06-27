namespace Musoq.Parser.Tokens;

public class AsOfJoinToken(bool isLeft, TextSpan span) : Token(TokenText, TokenType.AsOfJoin, span)
{
    public const string TokenText = "asof join";

    public bool IsLeft { get; } = isLeft;
}
