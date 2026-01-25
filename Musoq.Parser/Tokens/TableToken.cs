namespace Musoq.Parser.Tokens;

public class TableToken : Token
{
    public const string TokenText = "table";

    public TableToken(TextSpan textSpan)
        : base(TokenText, TokenType.Table, textSpan)
    {
    }
}
