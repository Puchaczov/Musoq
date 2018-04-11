using System;

namespace Musoq.Parser.Nodes
{
    public class TakeNode : UnaryNode
    {
        public TakeNode(IntegerNode expression) : base(expression)
        {
            Id = $"{nameof(TakeNode)}{ReturnType.Name}{Expression.Id}";
            Value = expression.Value;
        }

        public override Type ReturnType => typeof(long);

        public long Value { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
        public override string ToString()
        {
            return $"take {Expression.ToString()}";
        }
    }
}
