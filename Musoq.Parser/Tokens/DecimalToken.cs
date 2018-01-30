namespace FQL.Parser.Tokens
{
    public class DecimalToken : Token
    {
        public const string TokenText = "numeric";

        public DecimalToken(string value, TextSpan span)
            : base(value, TokenType.Decimal, span)
        {
        }
    }
}