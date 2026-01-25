namespace Musoq.Parser.Tokens;

public class DescToken : Token
{
    public const string TokenText = "desc";

    public DescToken(TextSpan span)
        : base(TokenText, TokenType.Desc, span)
    {
    }
}
