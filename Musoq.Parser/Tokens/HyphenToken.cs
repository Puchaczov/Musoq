namespace Musoq.Parser.Tokens;

public class HyphenToken : Token
{
    public const string TokenText = "-";

    public HyphenToken(TextSpan span)
        : base(TokenText, TokenType.Hyphen, span)
    {
    }
}