namespace Musoq.Parser.Tokens;

public class OverToken(TextSpan span) : Token(TokenText, TokenType.Over, span)
{
    public const string TokenText = "over";
}
