namespace Musoq.Parser.Tokens;

public class CurrentRowToken(TextSpan span) : Token(TokenText, TokenType.CurrentRow, span)
{
    public const string TokenText = "current row";
}
