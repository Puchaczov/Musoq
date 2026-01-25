namespace Musoq.Parser.Tokens;

public class ColonToken : Token
{
    public const string TokenText = ":";

    public ColonToken(TextSpan textSpan) : base(TokenText, TokenType.Colon, textSpan)
    {
    }
}
