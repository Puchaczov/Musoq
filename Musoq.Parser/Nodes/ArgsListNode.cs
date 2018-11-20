using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class ArgsListNode : Node
    {
        public ArgsListNode(Node[] args)
        {
            Args = args;

            var argsId = args.Length == 0 ? string.Empty : args.Select(f => f.Id).Aggregate((a, b) => a + b);
            Id = $"{nameof(ArgsListNode)}{argsId}";
        }

        public static ArgsListNode Empty => new ArgsListNode(new Node[0]);

        public Node[] Args { get; }

        public override Type ReturnType => Args[0].ReturnType;

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var str = Args.Length == 0
                ? string.Empty
                : Args.Select(f => f.ToString()).Aggregate((a, b) => $"{a.ToString()}, {b.ToString()}");
            return str;
        }
    }
}