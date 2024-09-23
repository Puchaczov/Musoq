namespace Musoq.Parser.Tokens;

public class NullToken : Token
{
    public const string TokenText = "null";

    public NullToken(TextSpan span)
        : base(TokenText, TokenType.Null, span)
    {
    }
}