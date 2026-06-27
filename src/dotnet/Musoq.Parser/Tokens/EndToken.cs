namespace Musoq.Parser.Tokens;

public class EndToken(TextSpan span) : Token(TokenText, TokenType.End, span)
{
    public const string TokenText = "end";
}
