namespace Musoq.Parser.Tokens;

public class LessToken : Token
{
    public const string TokenText = "<";

    public LessToken(TextSpan span)
        : base(TokenText, TokenType.Less, span)
    {
    }
}
