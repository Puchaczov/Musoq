using System;

namespace Musoq.Parser.Nodes
{
    public class IdentifierNode : Node
    {
        public IdentifierNode(string name, Type returnType = null)
        {
            Name = name;
            ReturnType = returnType;
            Id = $"{nameof(IdentifierNode)}{Name}";
        }

        public string Name { get; }
        public override Type ReturnType { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}