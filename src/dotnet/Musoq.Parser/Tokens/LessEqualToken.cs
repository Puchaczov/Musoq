namespace Musoq.Parser.Tokens;

public class LessEqualToken : Token
{
    public const string TokenText = "<=";

    public LessEqualToken(TextSpan span)
        : base(TokenText, TokenType.LessEqual, span)
    {
    }
}
