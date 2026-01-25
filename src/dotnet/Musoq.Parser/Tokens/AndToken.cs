namespace Musoq.Parser.Tokens;

public class AndToken : Token
{
    public const string TokenText = "and";

    public AndToken(TextSpan span)
        : base(TokenText, TokenType.And, span)
    {
    }
}
