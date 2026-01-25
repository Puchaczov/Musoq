namespace Musoq.Parser.Tokens;

public class RightParenthesisToken : Token
{
    public const string TokenText = ")";

    public RightParenthesisToken(TextSpan textSpan)
        : base(TokenText, TokenType.RightParenthesis, textSpan)
    {
    }
}
