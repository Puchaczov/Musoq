namespace Musoq.Parser.Tokens;

public class ThenToken : Token
{
    public const string TokenText = "then";

    public ThenToken(TextSpan span)
        : base(TokenText, TokenType.Then, span)
    {
    }
}
