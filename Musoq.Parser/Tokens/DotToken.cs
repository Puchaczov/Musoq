namespace Musoq.Parser.Tokens;

public class DotToken : Token
{
    public const string TokenText = ".";

    public DotToken(TextSpan span)
        : base(TokenText, TokenType.Dot, span)
    {
    }
}