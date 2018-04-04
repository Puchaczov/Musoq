namespace Musoq.Parser.Tokens
{
    public class OrderByToken : Token
    {
        public const string TokenText = "order by";

        public OrderByToken(TextSpan span)
            : base(TokenText, TokenType.OrderBy, span)
        {
        }
    }
}