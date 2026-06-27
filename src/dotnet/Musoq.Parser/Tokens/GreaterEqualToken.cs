namespace Musoq.Parser.Tokens;

public class GreaterEqualToken(TextSpan span) : Token(TokenText, TokenType.GreaterEqual, span)
{
    public const string TokenText = ">=";
}
