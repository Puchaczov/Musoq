namespace FQL.Parser.Tokens
{
    public class FunctionToken : Token
    {
        public const string TokenText = "function";

        public FunctionToken(string fname, TextSpan span)
            : base(fname, TokenType.Function, span)
        {
        }

        public override string ToString()
        {
            return Value;
        }
    }
}