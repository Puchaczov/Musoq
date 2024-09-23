namespace Musoq.Parser.Tokens;

public class StarToken : Token
{
    public const string TokenText = "*";

    public StarToken(TextSpan span)
        : base(TokenText, TokenType.Star, span)
    {
    }
}