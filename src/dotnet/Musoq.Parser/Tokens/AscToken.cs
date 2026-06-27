namespace Musoq.Parser.Tokens;

public class AscToken(TextSpan span) : Token(TokenText, TokenType.Asc, span)
{
    public const string TokenText = "asc";
}
