namespace Musoq.Parser.Tokens;

public class FSlashToken(TextSpan span) : Token(TokenText, TokenType.FSlash, span)
{
    public const string TokenText = "/";
}
