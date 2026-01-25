namespace Musoq.Parser.Tokens;

public class SkipToken : Token
{
    public const string TokenText = "skip";

    public SkipToken(string value, TextSpan span) : base(value, TokenType.Skip, span)
    {
    }
}
