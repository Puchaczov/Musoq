namespace Musoq.Parser.Tokens;

public class MethodAccessToken(string value, TextSpan span) : Token(value, TokenType.MethodAccess, span)
{
    public string Alias { get; } = value;
}
