namespace Musoq.Parser.Tokens;

public class UnpivotToken(TextSpan span) : Token(TokenText, TokenType.Unpivot, span)
{
    public const string TokenText = "unpivot";
}
