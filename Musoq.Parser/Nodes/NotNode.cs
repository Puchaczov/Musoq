using System;

namespace Musoq.Parser.Nodes
{
    public class NotNode : UnaryNode
    {
        public NotNode(Node expression)
            : base(expression)
        {
            Id = CalculateId(this);
        }

        public override Type ReturnType => Expression.ReturnType;

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"not ({Expression.ToString()})";
        }
    }
}