namespace FQL.Parser.Tokens
{
    public class WhereToken : Token
    {
        public const string TokenText = "where";

        public WhereToken(TextSpan span)
            : base(TokenText, TokenType.Where, span)
        {
        }
    }
}