namespace Musoq.Parser.Tokens;

public class LessEqualToken(TextSpan span) : Token(TokenText, TokenType.LessEqual, span)
{
    public const string TokenText = "<=";
}
