namespace Musoq.Parser.Tokens
{
    public class VarArgToken : Token
    {
        public const string TokenText = "arg";

        public VarArgToken(int argsCount)
            : base("arg", TokenType.VarArg, new TextSpan(0, 0))
        {
            Arguments = argsCount;
        }

        public VarArgToken(string name)
            : base(name, TokenType.VarArg, new TextSpan(0, 0))
        {
        }

        private int Arguments { get; }
    }
}