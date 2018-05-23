namespace Musoq.Parser.Nodes
{
    public class InMemoryGroupedFromNode : FromNode
    {
        public InMemoryGroupedFromNode(string alias)
            : base(alias)
        {
            Id = $"{nameof(InMemoryTableFromNode)}{Alias}";
        }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Alias;
        }
    }
}