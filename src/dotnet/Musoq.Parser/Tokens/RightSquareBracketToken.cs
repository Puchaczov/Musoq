namespace Musoq.Parser.Tokens;

public class RightSquareBracketToken : Token
{
    public const string TokenText = "]";

    public RightSquareBracketToken(TextSpan textSpan)
        : base(TokenText, TokenType.RightSquareBracket, textSpan)
    {
    }
}
