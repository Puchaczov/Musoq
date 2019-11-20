using System;

namespace Musoq.Parser.Nodes
{
    public class FieldLinkNode : Node
    {
        public FieldLinkNode(string value, Type returnType = null)
        {
            ReturnType = returnType;
            Index = int.Parse(value.Trim(':'));
        }

        public override Type ReturnType { get; }

        public int Index { get; }

        public override string Id => $"::{Index}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Id;
        }
    }
}