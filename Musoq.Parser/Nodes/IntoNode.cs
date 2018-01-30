using System;

namespace FQL.Parser.Nodes
{
    public class IntoNode : Node
    {
        public IntoNode(string name)
        {
            Name = name;
            Id = $"{nameof(IntoNode)}{name}";
        }

        public string Name { get; }

        public override Type ReturnType => null;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"into {Name}";
        }
    }
}