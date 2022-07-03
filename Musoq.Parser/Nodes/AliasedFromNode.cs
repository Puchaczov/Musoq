namespace Musoq.Parser.Nodes
{
    public class AliasedFromNode : FromNode
    {
        public AliasedFromNode(string identifier, ArgsListNode args, string alias)
            : base(alias)
        {
            Identifier = identifier;
            Args = args;
        }

        public string Identifier { get; }

        public ArgsListNode Args { get; }

        public override string Id => Identifier;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Identifier}({Args.ToString()})";
        }
    }
}
