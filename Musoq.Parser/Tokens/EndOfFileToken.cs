namespace Musoq.Parser.Tokens
{
    public class EndOfFileToken : Token
    {
        public const string TokenText = "eof";

        public EndOfFileToken(TextSpan span)
            : base(string.Empty, TokenType.EndOfFile, span)
        {
        }
    }
}