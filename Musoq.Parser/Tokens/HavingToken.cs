namespace Musoq.Parser.Tokens;

public class HavingToken : Token
{
    public const string TokenText = "having";

    public HavingToken(TextSpan span)
        : base(TokenText, TokenType.Having, span)
    {
    }
}