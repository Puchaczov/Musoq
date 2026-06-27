namespace Musoq.Parser.Tokens;

public class FalseToken(TextSpan span) : Token(TokenText, TokenType.False, span)
{
    public const string TokenText = "false";
}
