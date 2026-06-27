namespace Musoq.Parser.Tokens;

public class CrossApplyToken(TextSpan span) : Token(TokenText, TokenType.CrossApply, span)
{
    public const string TokenText = "cross apply";
}
