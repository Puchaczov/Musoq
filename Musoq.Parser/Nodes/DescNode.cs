using System;

namespace Musoq.Parser.Nodes
{
    public class DescNode : Node
    {
        public DescNode(FromNode from, DescForType type)
        {
            From = from;
            Id = $"{nameof(DescNode)}{from.Id}";
            Type = type;
        }

        public DescForType Type { get; set; }

        public FromNode From { get; }

        public override Type ReturnType { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"desc {From.ToString()}";
        }
    }
}