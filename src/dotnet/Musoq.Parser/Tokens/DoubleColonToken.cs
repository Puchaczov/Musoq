namespace Musoq.Parser.Tokens;

public class DoubleColonToken(TextSpan textSpan) : Token(TokenText, TokenType.DoubleColon, textSpan)
{
    public const string TokenText = "::";
}
