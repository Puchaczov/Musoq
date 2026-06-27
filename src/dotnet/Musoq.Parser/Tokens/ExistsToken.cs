namespace Musoq.Parser.Tokens;

public class ExistsToken(TextSpan span) : Token(TokenText, TokenType.Exists, span)
{
    public const string TokenText = "exists";
}
