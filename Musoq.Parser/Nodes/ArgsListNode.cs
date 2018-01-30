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

        public Node[] Args { get; }

        public override Type ReturnType => Args[0].ReturnType;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return Args.Select(f => f.ToString()).Aggregate((a, b) => $"{a.ToString()}, {b.ToString()}");
        }
    }
}