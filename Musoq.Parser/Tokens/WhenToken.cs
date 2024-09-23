namespace Musoq.Parser.Tokens;

public class WhenToken : Token
{
    public const string TokenText = "when";

    public WhenToken(TextSpan span)
        : base(TokenText, TokenType.When, span)
    {
    }
}