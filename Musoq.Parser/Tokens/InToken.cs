namespace Musoq.Parser.Tokens;

public class InToken : Token
{
    public const string TokenText = "in";

    public InToken(TextSpan span) 
        : base(TokenText, TokenType.In, span)
    {
    }
}