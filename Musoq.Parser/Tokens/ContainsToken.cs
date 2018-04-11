namespace Musoq.Parser.Tokens
{
    public class ContainsToken : Token
    {
        public const string TokenText = "contains";

        public ContainsToken(TextSpan span) : base(TokenText, TokenType.Contains, span)
        { }
    }
}
