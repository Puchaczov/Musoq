namespace Musoq.Parser.Tokens;

public class NoneToken() : Token(string.Empty, TokenType.None, new TextSpan(0, 0))
{
    public const string TokenText = "none";
}
