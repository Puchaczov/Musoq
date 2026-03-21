namespace Musoq.Parser.Tokens;

public class ExcludeToken : Token
{
    public const string TokenText = "exclude";

    public ExcludeToken(TextSpan span)
        : base(TokenText, TokenType.Exclude, span)
    {
    }
}
