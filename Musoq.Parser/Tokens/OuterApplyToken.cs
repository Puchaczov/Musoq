namespace Musoq.Parser.Tokens;

public class OuterApplyToken : Token
{
    public const string TokenText = "outer apply";

    public OuterApplyToken(TextSpan span)
        : base(TokenText, TokenType.OuterApply, span)
    {
    }
}