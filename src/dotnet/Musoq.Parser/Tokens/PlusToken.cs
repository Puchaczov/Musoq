namespace Musoq.Parser.Tokens;

public class PlusToken(TextSpan span) : Token(TokenText, TokenType.Plus, span)
{
    public const string TokenText = "+";
}
