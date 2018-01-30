namespace FQL.Parser.Tokens
{
    public class GreaterToken : Token
    {
        public const string TokenText = ">";

        public GreaterToken(TextSpan span)
            : base(TokenText, TokenType.Greater, span)
        {
        }
    }
}