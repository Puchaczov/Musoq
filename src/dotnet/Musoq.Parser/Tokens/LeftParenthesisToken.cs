namespace Musoq.Parser.Tokens;

public class LeftParenthesisToken(TextSpan textSpan) : Token(TokenText, TokenType.LeftParenthesis, textSpan)
{
    public const string TokenText = "(";
}
