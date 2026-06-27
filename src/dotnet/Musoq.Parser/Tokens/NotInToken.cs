namespace Musoq.Parser.Tokens;

public class NotInToken(TextSpan span) : Token(TokenText, TokenType.NotIn, span)
{
    public const string TokenText = "not in";
}
