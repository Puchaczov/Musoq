namespace Musoq.Parser.Tokens;

public class ColumnKeywordToken : Token
{
    public const string TokenText = "column";

    public ColumnKeywordToken(TextSpan span)
        : base(TokenText, TokenType.ColumnKeyword, span)
    {
    }
}
