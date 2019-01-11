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

    public class StatementNode : Node
    {
        public StatementNode(Node node)
        {
            ReturnType = typeof(void);
            Node = node;
            Id = null;
        }

        public Node Node { get; }

        public override Type ReturnType { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Node.ToString();
        }
    }

    public class StatementsArrayNode : Node
    {
        public StatementsArrayNode(StatementNode[] nodes)
        {
            ReturnType = typeof(void);
            Nodes = nodes;
            Id = null;
        }

        public StatementNode[] Nodes { get; }

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