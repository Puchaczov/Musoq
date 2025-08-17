namespace Musoq.Parser.Tokens;

public class PivotToken : Token
{
    public const string TokenText = "pivot";

    public PivotToken(TextSpan span)
        : base(TokenText, TokenType.Pivot, span)
    {
    }
}