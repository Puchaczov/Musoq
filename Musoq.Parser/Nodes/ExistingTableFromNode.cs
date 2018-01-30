namespace Musoq.Parser.Nodes
{
    public class ExistingTableFromNode : FromNode
    {
        public ExistingTableFromNode(string schema, string method) 
            : base(schema, method, new string[0])
        {
            Id = $"{nameof(ExistingTableFromNode)}{schema}{method}";
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
    }
}