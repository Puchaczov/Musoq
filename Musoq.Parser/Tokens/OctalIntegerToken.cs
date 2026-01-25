namespace Musoq.Parser.Tokens;

public class OctalIntegerToken : Token
{
    public const string TokenText = "octal numeric";

    public OctalIntegerToken(string value, TextSpan span)
        : base(value, TokenType.OctalInteger, span)
    {
    }
}
