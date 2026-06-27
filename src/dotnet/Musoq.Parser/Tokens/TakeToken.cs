namespace Musoq.Parser.Tokens;

public class TakeToken(string value, TextSpan span) : Token(value, TokenType.Take, span)
{
    public const string TokenText = "take";
}
