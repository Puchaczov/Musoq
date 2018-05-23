using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class MultiStatementNode : Node
    {
        public MultiStatementNode(Node[] nodes, Type returnType)
        {
            ReturnType = returnType;
            Nodes = nodes;
            Id = null;
        }

        public Node[] Nodes { get; }

        public override Type ReturnType { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Nodes.Select(f => f.ToString())
                .Aggregate((a, b) => $"{a.ToString()}{Environment.NewLine}{b.ToString()}");
        }
    }
}