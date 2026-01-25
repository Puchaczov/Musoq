namespace Musoq.Parser.Tokens;

public class RBracketToken : Token
{
    public const string TokenText = "}";

    public RBracketToken(TextSpan textSpan)
        : base(TokenText, TokenType.RBracket, textSpan)
    {
    }
}
