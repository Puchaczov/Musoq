namespace Musoq.Parser.Tokens;

public class UnionAllToken : SetOperatorToken
{
    public UnionAllToken(TextSpan span)
        : base(UnionAllOperatorText, TokenType.UnionAll, span)
    {
    }
}
