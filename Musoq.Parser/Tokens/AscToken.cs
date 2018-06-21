namespace Musoq.Parser.Tokens
{
    public class AscToken : Token
    {
        public const string TokenText = "asc";

        public AscToken(TextSpan span)
            : base(TokenText, TokenType.Asc, span)
        {
        }
    }
}