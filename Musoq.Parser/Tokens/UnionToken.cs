namespace Musoq.Parser.Tokens;

public class UnionToken : SetOperatorToken
{
    public UnionToken(TextSpan span)
        : base(UnionOperatorText, TokenType.Union, span)
    {
    }
}