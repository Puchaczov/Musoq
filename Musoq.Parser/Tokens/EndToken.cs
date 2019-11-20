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

    public class KFieldLinkToken : Token
    {
        public KFieldLinkToken(string value, TextSpan span) 
            : base(value, TokenType.FieldLink, span)
        {
        }
    }
}