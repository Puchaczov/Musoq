namespace Musoq.Parser.Tokens;

public class CrossApplyToken : Token
{
    public const string TokenText = "cross apply";

    public CrossApplyToken(TextSpan span)
        : base(TokenText, TokenType.CrossApply, span)
    {
    }
}
