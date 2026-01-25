namespace Musoq.Parser.Tokens;

public class HexIntegerToken : Token
{
    public const string TokenText = "hexadecimal numeric";

    public HexIntegerToken(string value, TextSpan span)
        : base(value, TokenType.HexadecimalInteger, span)
    {
    }
}
