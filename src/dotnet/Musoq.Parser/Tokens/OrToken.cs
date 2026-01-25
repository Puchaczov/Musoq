namespace Musoq.Parser.Tokens;

public class OrToken : Token
{
    public const string TokenText = "or";

    public OrToken(TextSpan span)
        : base(TokenText, TokenType.Or, span)
    {
    }
}
