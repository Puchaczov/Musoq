namespace Musoq.Parser.Tokens;

public class LBracketToken : Token
{
    public const string TokenText = "{";

    public LBracketToken(TextSpan textSpan)
        : base(TokenText, TokenType.LBracket, textSpan)
    {
    }
}