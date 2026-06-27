namespace Musoq.Parser.Tokens;

public class OuterApplyToken(TextSpan span) : Token(TokenText, TokenType.OuterApply, span)
{
    public const string TokenText = "outer apply";
}
