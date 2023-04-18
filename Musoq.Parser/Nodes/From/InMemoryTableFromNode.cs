using System;

namespace Musoq.Parser.Nodes.From
{
    public class InMemoryTableFromNode : FromNode
    {
        internal InMemoryTableFromNode(string variableName, string alias)
            : base(alias)
        {
            VariableName = variableName;
        }
        
        public InMemoryTableFromNode(string variableName, string alias, Type returnType)
            : base(alias, returnType)
        {
            VariableName = variableName;
        }

        public string VariableName { get; }

        public override string Id => $"{nameof(InMemoryTableFromNode)}{VariableName}{Alias}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"from {VariableName}";
        }
    }
}