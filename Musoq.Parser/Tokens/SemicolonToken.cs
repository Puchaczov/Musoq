namespace Musoq.Parser.Tokens;

public class SemicolonToken : Token
{
    public const string TokenText = ";";

    public SemicolonToken(TextSpan textSpan)
        : base(TokenText, TokenType.Semicolon, textSpan)
    {
    }
}