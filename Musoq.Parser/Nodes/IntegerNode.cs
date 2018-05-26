using System;

namespace Musoq.Parser.Nodes
{
    public class IntegerNode : ConstantValueNode
    {
        public IntegerNode(string value)
        {
            Value = Convert.ToInt64(value);
            Id = $"{nameof(IntegerNode)}{value}{ReturnType.Name}";
        }

        public long Value { get; }

        public override Type ReturnType => typeof(long);

        public override string Id { get; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}