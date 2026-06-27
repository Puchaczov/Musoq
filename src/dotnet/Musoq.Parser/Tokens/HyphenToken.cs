namespace Musoq.Parser.Tokens;

public class HyphenToken(TextSpan span) : Token(TokenText, TokenType.Hyphen, span)
{
    public const string TokenText = "-";
}
