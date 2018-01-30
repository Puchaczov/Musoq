using System;

namespace FQL.Parser.Nodes
{
    public class NotNode : UnaryNode
    {
        public NotNode(Node expression)
            : base(expression)
        {
            Id = CalculateId(this);
        }

        public override Type ReturnType => typeof(bool);

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"not ({Expression.ToString()})";
        }
    }
}