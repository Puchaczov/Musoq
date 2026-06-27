namespace Musoq.Parser.Tokens;

public class InToken(TextSpan span) : Token(TokenText, TokenType.In, span)
{
    public const string TokenText = "in";
}
