namespace Musoq.Parser.Tokens;

public class DistinctToken : Token
{
    public const string TokenText = "distinct";

    public DistinctToken(TextSpan span)
        : base(TokenText, TokenType.Distinct, span)
    {
    }
}