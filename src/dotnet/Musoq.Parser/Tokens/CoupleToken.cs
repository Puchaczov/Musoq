namespace Musoq.Parser.Tokens;

public class CoupleToken(TextSpan textSpan) : Token(TokenText, TokenType.Couple, textSpan)
{
    public const string TokenText = "couple";
}
