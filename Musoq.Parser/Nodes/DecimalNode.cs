using System;
using System.Globalization;

namespace Musoq.Parser.Nodes
{
    public class DecimalNode : ConstantValueNode
    {
        public DecimalNode(string value)
        {
            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                throw new NotSupportedException($"The value '{value}' cannot be converted to decimal.");

            Value = result;
            Id = $"{nameof(DecimalNode)}{value}{ReturnType.Name}";
        }

        public DecimalNode(decimal value)
        {
            Value = value;
            Id = $"{nameof(DecimalNode)}{value}{ReturnType.Name}";
        }

        public decimal Value { get; }

        public override object ObjValue => Value;

        public override Type ReturnType => typeof(decimal);

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