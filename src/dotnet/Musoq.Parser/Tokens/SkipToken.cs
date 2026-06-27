namespace Musoq.Parser.Tokens;

public class SkipToken(string value, TextSpan span) : Token(value, TokenType.Skip, span)
{
    public const string TokenText = "skip";
}
