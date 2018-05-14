using System;

namespace Musoq.Parser.Nodes
{
    public class DescNode : Node
    {
        public DescNode(FromNode from)
        {
            From = from;
            Id = $"{nameof(DescNode)}{from.Id}";
        }

        public FromNode From { get; }

        public override Type ReturnType { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"desc {From.ToString()}";
        }
    }
}