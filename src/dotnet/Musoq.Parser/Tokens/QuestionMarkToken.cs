namespace Musoq.Parser.Tokens;

public class QuestionMarkToken(TextSpan textSpan) : Token(TokenText, TokenType.QuestionMark, textSpan)
{
    public const string TokenText = "?";
}
