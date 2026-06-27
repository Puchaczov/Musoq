namespace Musoq.Parser.Tokens;

public class SemicolonToken(TextSpan textSpan) : Token(TokenText, TokenType.Semicolon, textSpan)
{
    public const string TokenText = ";";
}
