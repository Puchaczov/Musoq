namespace Musoq.Parser.Nodes
{
    public class CteFromNode : FromNode
    {

        public CteFromNode(string variableName) 
            : base(variableName, string.Empty, new string[0])
        {
            VariableName = variableName;
        }

        public string VariableName { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id => $"{nameof(CteFromNode)}{VariableName}";

        public override string ToString()
        {
            return $"from {VariableName}";
        }
    }
}