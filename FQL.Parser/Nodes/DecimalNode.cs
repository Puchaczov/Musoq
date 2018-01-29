using System;
using System.Globalization;

namespace FQL.Parser.Nodes
{
    public class DecimalNode : Node
    {
        public DecimalNode(string value)
        {
            Value = Convert.ToDecimal(value);
            Id = $"{nameof(DecimalNode)}{value}{ReturnType.Name}";
        }

        public decimal Value { get; }

        public override Type ReturnType => typeof(decimal);

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
    }
}