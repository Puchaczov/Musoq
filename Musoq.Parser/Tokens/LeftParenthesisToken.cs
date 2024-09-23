namespace Musoq.Parser.Tokens;

public class LeftParenthesisToken : Token
{
    public const string TokenText = "(";

    public LeftParenthesisToken(TextSpan textSpan)
        : base(TokenText, TokenType.LeftParenthesis, textSpan)
    {
    }
}