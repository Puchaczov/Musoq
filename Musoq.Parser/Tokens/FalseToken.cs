namespace Musoq.Parser.Tokens
{
    public class FalseToken : Token
    {
        public const string TokenText = "false";

        public FalseToken(TextSpan span)
            : base(TokenText, TokenType.False, span)
        {
        }
    }
}