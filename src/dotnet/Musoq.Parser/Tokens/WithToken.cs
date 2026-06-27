namespace Musoq.Parser.Tokens;

public class WithToken(TextSpan span) : Token(TokenText, TokenType.With, span)
{
    public const string TokenText = "with";
}
