namespace Musoq.Parser.Tokens;

public class WhenToken(TextSpan span) : Token(TokenText, TokenType.When, span)
{
    public const string TokenText = "when";
}
