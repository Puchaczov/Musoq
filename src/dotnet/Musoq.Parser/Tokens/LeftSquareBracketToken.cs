namespace Musoq.Parser.Tokens;

public class LeftSquareBracketToken : Token
{
    public const string TokenText = "[";

    public LeftSquareBracketToken(TextSpan textSpan)
        : base(TokenText, TokenType.LeftSquareBracket, textSpan)
    {
    }
}
