namespace Musoq.Parser.Tokens;

public class OrToken(TextSpan span) : Token(TokenText, TokenType.Or, span)
{
    public const string TokenText = "or";
}
