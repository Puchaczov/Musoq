namespace Musoq.Parser.Tokens;

public class LessToken(TextSpan span) : Token(TokenText, TokenType.Less, span)
{
    public const string TokenText = "<";
}
