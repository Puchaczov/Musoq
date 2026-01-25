namespace Musoq.Parser.Tokens;

public class CaretToken : Token
{
    public const string TokenText = "^";

    public CaretToken(TextSpan span)
        : base(TokenText, TokenType.Caret, span)
    {
    }
}
