namespace Musoq.Parser.Tokens;

public class AsOfJoinToken : Token
{
    public const string TokenText = "asof join";

    public AsOfJoinToken(bool isLeft, TextSpan span)
        : base(TokenText, TokenType.AsOfJoin, span)
    {
        IsLeft = isLeft;
    }

    public bool IsLeft { get; }
}
