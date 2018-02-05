namespace Musoq.Parser.Tokens
{
    public class TakeToken : Token
    {
        public const string TokenText = "take";

        public TakeToken(TextSpan span) : base(TokenText, TokenType.Take, span)
        {
        }
    }
}