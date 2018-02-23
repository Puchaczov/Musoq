namespace Musoq.Parser.Nodes
{
    public class VariableFromNode : FromNode
    {

        public VariableFromNode(string variableName) : base(string.Empty, string.Empty, new string[0])
        {
            VariableName = variableName;
        }

        public string VariableName { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id => $"{nameof(VariableFromNode)}{VariableName}";

        public override string ToString()
        {
            return $"from {VariableName}";
        }
    }
}