using System;
using System.Linq;

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
            var args = From.Parameters.Length == 0 ? string.Empty : From.Parameters.Aggregate((a, b) => $"'{a}'" + ',' + $"'{b}'");
            return $"desc {From.Schema}.{From.Method}({args})";
        }
    }
}