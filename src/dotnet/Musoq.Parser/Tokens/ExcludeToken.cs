namespace Musoq.Parser.Tokens;

public class ExcludeToken(TextSpan span) : Token(TokenText, TokenType.Exclude, span)
{
    public const string TokenText = "exclude";
}
