namespace Musoq.Parser.Tokens;

public class DistinctToken(TextSpan span) : Token(TokenText, TokenType.Distinct, span)
{
    public const string TokenText = "distinct";
}
