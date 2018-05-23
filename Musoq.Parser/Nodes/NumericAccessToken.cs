using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes
{
    public class NumericAccessToken : Token
    {
        public NumericAccessToken(string name, string value, TextSpan span)
            : base(value, TokenType.NumericAccess, span)
        {
            Index = int.Parse(value);
            Name = name;
        }

        public int Index { get; }

        public string Name { get; }
    }
}