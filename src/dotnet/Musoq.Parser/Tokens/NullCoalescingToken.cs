namespace Musoq.Parser.Tokens;

public class NullCoalescingToken(TextSpan textSpan) : Token(TokenText, TokenType.NullCoalescing, textSpan)
{
    public const string TokenText = "??";
}