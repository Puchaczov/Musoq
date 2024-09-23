namespace Musoq.Parser.Tokens;

public class OnToken : Token
{
    public const string TokenText = "on";

    public OnToken(TextSpan span)
        : base(TokenText, TokenType.On, span)
    {
    }
}