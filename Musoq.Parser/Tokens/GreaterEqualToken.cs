namespace Musoq.Parser.Tokens
{
    public class GreaterEqualToken : Token
    {
        public const string TokenText = ">=";

        public GreaterEqualToken(TextSpan span)
            : base(TokenText, TokenType.GreaterEqual, span)
        {
        }
    }
}