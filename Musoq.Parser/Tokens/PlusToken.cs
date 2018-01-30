namespace FQL.Parser.Tokens
{
    public class PlusToken : Token
    {
        public const string TokenText = "+";

        public PlusToken(TextSpan span)
            : base(TokenText, TokenType.Plus, span)
        {
        }
    }
}