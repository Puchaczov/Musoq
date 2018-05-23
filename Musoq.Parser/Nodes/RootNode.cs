using System;

namespace Musoq.Parser.Nodes
{
    public class RootNode : UnaryNode
    {
        public RootNode(Node expression) : base(expression)
        {
            Id = $"{nameof(RootNode)}{expression.Id}";
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