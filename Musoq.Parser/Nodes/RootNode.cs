using System;

namespace Musoq.Parser.Nodes
{
    public class RootNode : UnaryNode
    {
        public RootNode(Node node) : base(node)
        {
            Id = $"{nameof(RootNode)}{node.Id}";
        }

        public override Type ReturnType => null;

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }
}