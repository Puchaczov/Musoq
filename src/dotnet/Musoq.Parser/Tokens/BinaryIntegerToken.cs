namespace Musoq.Parser.Tokens;

public class BinaryIntegerToken(string value, TextSpan span) : Token(value, TokenType.BinaryInteger, span)
{
    public const string TokenText = "binary numeric";
}
