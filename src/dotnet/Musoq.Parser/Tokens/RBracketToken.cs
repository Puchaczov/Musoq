namespace Musoq.Parser.Tokens;

public class RBracketToken(TextSpan textSpan) : Token(TokenText, TokenType.RBracket, textSpan)
{
    public const string TokenText = "}";
}
