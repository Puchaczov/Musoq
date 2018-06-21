using System;
using System.Globalization;

namespace Musoq.Parser.Nodes
{
    public class BooleanNode : ConstantValueNode
    {
        public BooleanNode(bool value)
        {
            Value = value;
            Id = $"{nameof(BooleanNode)}{value}{ReturnType.Name}";
        }

        public bool Value { get; }

        public override Type ReturnType => typeof(bool);

        public override string Id { get; }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}