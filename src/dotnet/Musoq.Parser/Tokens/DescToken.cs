namespace Musoq.Parser.Tokens;

public class DescToken(TextSpan span) : Token(TokenText, TokenType.Desc, span)
{
    public const string TokenText = "desc";
}
