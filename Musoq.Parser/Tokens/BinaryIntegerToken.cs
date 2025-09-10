namespace Musoq.Parser.Tokens;

public class BinaryIntegerToken : Token
{
    public const string TokenText = "binary numeric";

    public BinaryIntegerToken(string value, TextSpan span)
        : base(value, TokenType.BinaryInteger, span)
    {
    }
}