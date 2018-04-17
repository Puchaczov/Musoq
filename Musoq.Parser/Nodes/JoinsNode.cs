using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class JoinsNode : Node {

        public JoinsNode(JoinNode[] args)
        {
            Joins = args;

            var argsId = args.Length == 0 ? string.Empty : args.Select(f => f.Id).Aggregate((a, b) => a + b);
            Id = $"{nameof(JoinsNode)}{argsId}";
        }

        public JoinNode[] Joins { get; }

        public override Type ReturnType => typeof(void);

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            var str = Joins.Length == 0
                ? string.Empty
                : Joins.Select(f => f.ToString()).Aggregate((a, b) => $"{a.ToString()}, {b.ToString()}");
            return str;
        }
    }
}