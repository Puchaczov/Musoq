namespace Musoq.Parser.Tokens;

public class OctalIntegerToken(string value, TextSpan span) : Token(value, TokenType.OctalInteger, span)
{
    public const string TokenText = "octal numeric";
}
