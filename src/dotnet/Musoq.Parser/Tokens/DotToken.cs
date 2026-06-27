namespace Musoq.Parser.Tokens;

public class DotToken(TextSpan span) : Token(TokenText, TokenType.Dot, span)
{
    public const string TokenText = ".";
}
