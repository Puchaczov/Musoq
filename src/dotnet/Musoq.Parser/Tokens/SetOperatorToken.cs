namespace Musoq.Parser.Tokens;

public class SetOperatorToken(string setOperator, TokenType type, TextSpan span) : Token(setOperator, type, span)
{
    public const string ExceptOperatorText = "except";
    public const string UnionOperatorText = "union";
    public const string IntersectOperatorText = "intersect";
    public const string UnionAllOperatorText = "union all";
}
