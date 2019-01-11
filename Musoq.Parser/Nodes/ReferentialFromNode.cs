namespace Musoq.Parser.Nodes
{
    public class ReferentialFromNode : FromNode
    {
        public ReferentialFromNode(string name, string alias)
            : base(alias)
        {
            Name = name;
        }

        public string Name { get; }

        public override string Id => $"{Name}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
