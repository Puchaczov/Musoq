namespace Musoq.Parser.Tokens;

public class MissingToken(TextSpan span) : Token(TokenText, TokenType.Missing, span)
{
    public const string TokenText = "missing";
}
