namespace Musoq.Parser.Tokens
{
    public class EndToken : Token
    {
        public const string TokenText = "end";

        public EndToken(TextSpan span)
            : base(TokenText, TokenType.End, span)
        {
        }
    }
}