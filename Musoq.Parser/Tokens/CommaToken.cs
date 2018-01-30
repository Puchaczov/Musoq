namespace Musoq.Parser.Tokens
{
    public class CommaToken : Token
    {
        public const string TokenText = ",";

        public CommaToken(TextSpan span)
            : base(TokenText, TokenType.Comma, span)
        {
        }
    }
}