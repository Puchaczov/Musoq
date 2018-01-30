namespace Musoq.Parser.Tokens
{
    public class IntegerToken : Token
    {
        public const string TokenText = "numeric";

        public IntegerToken(string value, TextSpan span)
            : base(value, TokenType.Integer, span)
        {
        }
    }
}