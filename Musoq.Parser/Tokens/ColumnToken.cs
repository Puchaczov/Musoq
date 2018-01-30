namespace Musoq.Parser.Tokens
{
    public class ColumnToken : Token
    {
        public ColumnToken(string value, TextSpan span)
            : base(value, TokenType.Column, span)
        {
        }
    }
}