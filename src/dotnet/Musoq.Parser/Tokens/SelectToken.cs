namespace Musoq.Parser.Tokens;

public class SelectToken(TextSpan span) : Token(TokenText, TokenType.Select, span)
{
    public const string TokenText = "select";
}
