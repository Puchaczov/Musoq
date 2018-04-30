namespace Musoq.Parser.Nodes
{
    public class ExistingTableFromNode : FromNode
    {
        public ExistingTableFromNode() 
            : base(string.Empty)
        {
            Id = $"{nameof(ExistingTableFromNode)}";
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            throw new System.NotImplementedException();
        }

        public override string Id { get; }
    }
}