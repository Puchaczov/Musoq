namespace Musoq.Parser.Tokens;

public class FromToken : Token
{
    public const string TokenText = "from";

    public FromToken(TextSpan span)
        : base(TokenText, TokenType.From, span)
    {
    }
}