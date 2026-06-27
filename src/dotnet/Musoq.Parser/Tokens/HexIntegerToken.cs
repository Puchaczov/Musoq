namespace Musoq.Parser.Tokens;

public class HexIntegerToken(string value, TextSpan span) : Token(value, TokenType.HexadecimalInteger, span)
{
    public const string TokenText = "hexadecimal numeric";
}
