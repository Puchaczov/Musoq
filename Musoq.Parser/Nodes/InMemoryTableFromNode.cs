namespace Musoq.Parser.Nodes
{
    public class InMemoryTableFromNode : FromNode
    {

        public InMemoryTableFromNode(string variableName, string alias = "") 
            : base(alias)
        {
            VariableName = variableName;
        }

        public string VariableName { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id => $"{nameof(InMemoryTableFromNode)}{VariableName}";

        public override string ToString()
        {
            return $"from {VariableName}";
        }
    }
}