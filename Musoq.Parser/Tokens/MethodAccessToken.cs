namespace Musoq.Parser.Tokens
{
    public class MethodAccessToken : Token
    {
        public MethodAccessToken(string value, TextSpan span)
            : base(value, TokenType.MethodAccess, span)
        {
            Alias = value;
        }

        public string Alias { get; }
    }
}
