namespace Musoq.Parser.Tokens;

public class AsToken(TextSpan span) : Token(TokenText, TokenType.As, span)
{
    public const string TokenText = "as";
}
