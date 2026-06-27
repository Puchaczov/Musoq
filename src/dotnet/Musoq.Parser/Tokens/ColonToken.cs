namespace Musoq.Parser.Tokens;

public class ColonToken(TextSpan textSpan) : Token(TokenText, TokenType.Colon, textSpan)
{
    public const string TokenText = ":";
}
