namespace Musoq.Parser.Tokens;

public class FromToken(TextSpan span) : Token(TokenText, TokenType.From, span)
{
    public const string TokenText = "from";
}
