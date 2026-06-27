namespace Musoq.Parser.Tokens;

public class LeftSquareBracketToken(TextSpan textSpan) : Token(TokenText, TokenType.LeftSquareBracket, textSpan)
{
    public const string TokenText = "[";
}
