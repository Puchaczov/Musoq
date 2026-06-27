namespace Musoq.Parser.Tokens;

public class ColumnKeywordToken(TextSpan span) : Token(TokenText, TokenType.ColumnKeyword, span)
{
    public const string TokenText = "column";
}
