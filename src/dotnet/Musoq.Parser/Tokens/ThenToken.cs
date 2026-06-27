namespace Musoq.Parser.Tokens;

public class ThenToken(TextSpan span) : Token(TokenText, TokenType.Then, span)
{
    public const string TokenText = "then";
}
