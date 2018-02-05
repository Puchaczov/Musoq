using System;

namespace Musoq.Parser.Nodes
{
    public class SkipNode : UnaryNode
    {
        public SkipNode(IntegerNode expression) : base(expression)
        {
            Id = $"{nameof(SkipNode)}{ReturnType.Name}{Expression.Id}";
            Value = expression.Value;
        }

        public long Value { get; }

        public override Type ReturnType => typeof(long);

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
        public override string ToString()
        {
            return $"skip {Expression.ToString()}";
        }
    }
}