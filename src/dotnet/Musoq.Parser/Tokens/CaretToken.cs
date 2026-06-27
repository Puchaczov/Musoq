namespace Musoq.Parser.Tokens;

public class CaretToken(TextSpan span) : Token(TokenText, TokenType.Caret, span)
{
    public const string TokenText = "^";
}
