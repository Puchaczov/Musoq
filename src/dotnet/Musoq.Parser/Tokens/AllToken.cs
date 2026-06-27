namespace Musoq.Parser.Tokens;

public class AllToken(TextSpan span) : Token(TokenText, TokenType.All, span)
{
    public const string TokenText = "all";
}
