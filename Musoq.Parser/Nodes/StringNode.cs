using System;

namespace Musoq.Parser.Nodes
{
    public class StringNode : ConstantValueNode
    {
        public StringNode(string value)
        {
            Value = value;
            Id = $"{nameof(StringNode)}{ReturnType.Name}{value}";
        }

        public string Value { get; }

        public override Type ReturnType => typeof(string);

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"'{Value}'";
        }
    }
}