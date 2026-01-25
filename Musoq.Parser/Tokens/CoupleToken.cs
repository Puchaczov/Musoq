namespace Musoq.Parser.Tokens;

public class CoupleToken : Token
{
    public const string TokenText = "couple";

    private TextSpan textSpan;

    public CoupleToken(TextSpan textSpan)
        : base(TokenText, TokenType.Couple, textSpan)
    {
        this.textSpan = textSpan;
    }
}
