namespace Musoq.Parser.Tokens;

public class LBracketToken(TextSpan textSpan) : Token(TokenText, TokenType.LBracket, textSpan)
{
    public const string TokenText = "{";
}
