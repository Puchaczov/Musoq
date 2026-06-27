namespace Musoq.Parser.Tokens;

public class RightSquareBracketToken(TextSpan textSpan) : Token(TokenText, TokenType.RightSquareBracket, textSpan)
{
    public const string TokenText = "]";
}
