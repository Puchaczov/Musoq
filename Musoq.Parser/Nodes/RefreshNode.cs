using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class RefreshNode : Node
    {
        public RefreshNode(AccessMethodNode[] nodes)
        {
            Nodes = nodes;
            var nodesId = nodes.Length == 0 ? string.Empty : nodes.Select(f => f.Id).Aggregate((a, b) => a + b);
            Id = $"{nameof(RefreshNode)}{nodesId}";
        }

        public AccessMethodNode[] Nodes { get; }

        public override Type ReturnType => null;

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var methods = Nodes.Length == 0
                ? string.Empty
                : Nodes.Select(f => f.ToString()).Aggregate((a, b) => $"{a}, {b}");

            return $"refresh ({methods})";
        }
    }
}