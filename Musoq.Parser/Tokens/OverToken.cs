namespace Musoq.Parser.Tokens;

public class OverToken : Token
{
    public const string TokenText = "over";

    public OverToken(TextSpan span)
        : base(TokenText, TokenType.Over, span)
    {
    }
}