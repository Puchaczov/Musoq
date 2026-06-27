namespace Musoq.Parser.Tokens;

public class HavingToken(TextSpan span) : Token(TokenText, TokenType.Having, span)
{
    public const string TokenText = "having";
}
