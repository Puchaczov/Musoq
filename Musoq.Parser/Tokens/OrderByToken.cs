namespace Musoq.Parser.Tokens
{
    public class OrderByToken : Token
    {
        public static string TokenText = "order by";

        public OrderByToken(TextSpan span)
            : base(TokenText, TokenType.OrderBy, span)
        {
        }
    }
}
