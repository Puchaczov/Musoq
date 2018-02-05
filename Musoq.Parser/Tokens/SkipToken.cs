namespace Musoq.Parser.Tokens
{
    public class SkipToken : Token
    {
        public const string TokenText = "skip";

        public SkipToken(TextSpan span) : base(TokenText, TokenType.Skip, span)
        {
        }
    }
}