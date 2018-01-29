namespace FQL.Parser.Tokens
{
    public class IntersectToken : SetOperatorToken
    {
        public IntersectToken(TextSpan span)
            : base(IntersectOperatorText, TokenType.Intersect, span)
        {
        }
    }
}