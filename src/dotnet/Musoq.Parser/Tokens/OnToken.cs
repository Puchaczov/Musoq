namespace Musoq.Parser.Tokens;

public class OnToken(TextSpan span) : Token(TokenText, TokenType.On, span)
{
    public const string TokenText = "on";
}
