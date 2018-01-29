namespace FQL.Parser.Tokens
{
    public class NumericColumnToken : Token
    {
        public NumericColumnToken(string value, TextSpan span)
            : base(value, TokenType.NumericColumn, span)
        {
        }

        public int Index => int.Parse(Value);
    }
}