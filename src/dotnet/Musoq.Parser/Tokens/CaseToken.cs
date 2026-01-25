namespace Musoq.Parser.Tokens;

public class CaseToken : Token
{
    public const string TokenText = "case";

    public CaseToken(TextSpan span)
        : base(TokenText, TokenType.Case, span)
    {
    }
}
