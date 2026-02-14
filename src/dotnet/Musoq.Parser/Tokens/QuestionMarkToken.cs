namespace Musoq.Parser.Tokens;

public class QuestionMarkToken : Token
{
    public const string TokenText = "?";

    public QuestionMarkToken(TextSpan textSpan)
        : base(TokenText, TokenType.QuestionMark, textSpan)
    {
    }
}
