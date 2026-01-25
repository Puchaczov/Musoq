namespace Musoq.Parser.Tokens;

public class FieldLinkToken : Token
{
    public FieldLinkToken(string value, TextSpan span)
        : base(value, TokenType.FieldLink, span)
    {
    }
}
