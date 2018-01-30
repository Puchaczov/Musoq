namespace Musoq.Parser.Tokens
{
    public class SetOperatorToken : Token
    {
        public const string ExceptOperatorText = "except";
        public const string UnionOperatorText = "union";
        public const string IntersectOperatorText = "intersect";
        public const string UnionAllOperatorText = "union all";

        public SetOperatorToken(string setOperator, TokenType type, TextSpan span)
            : base(setOperator, type, span)
        {
        }
    }
}