using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class JoinsNode : FromNode {

        public JoinsNode(JoinFromNode joins)
            : base(joins.Alias)
        {
            Id = $"{nameof(JoinsNode)}{joins.Id}";
        }

        public JoinFromNode Joins { get; }

        public override Type ReturnType => typeof(void);

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return Joins.ToString();
        }
    }
}