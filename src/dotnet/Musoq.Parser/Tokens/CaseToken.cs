namespace Musoq.Parser.Tokens;

public class CaseToken(TextSpan span) : Token(TokenText, TokenType.Case, span)
{
    public const string TokenText = "case";
}
