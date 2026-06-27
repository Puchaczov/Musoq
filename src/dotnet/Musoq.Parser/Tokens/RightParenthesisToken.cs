namespace Musoq.Parser.Tokens;

public class RightParenthesisToken(TextSpan textSpan) : Token(TokenText, TokenType.RightParenthesis, textSpan)
{
    public const string TokenText = ")";
}
