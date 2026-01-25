namespace Musoq.Parser.Tokens;

public class IsToken : Token
{
    public const string TokenText = "is";

    public IsToken(TextSpan span)
        : base(TokenText, TokenType.Is, span)
    {
    }
}
