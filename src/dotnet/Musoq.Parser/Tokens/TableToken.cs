namespace Musoq.Parser.Tokens;

public class TableToken(TextSpan textSpan) : Token(TokenText, TokenType.Table, textSpan)
{
    public const string TokenText = "table";
}
