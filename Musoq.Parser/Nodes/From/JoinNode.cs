using System;

namespace Musoq.Parser.Nodes.From
{
    public class JoinNode : FromNode
    {
        internal JoinNode(JoinFromNode joins)
            : base(joins.Alias)
        {
            Id = $"{nameof(JoinNode)}{joins.Id}";
            Join = joins;
        }
        
        public JoinNode(JoinFromNode joins, Type returnType)
            : base(joins.Alias, returnType)
        {
            Id = $"{nameof(JoinNode)}{joins.Id}";
            Join = joins;
        }

        public JoinFromNode Join { get; }

        public override Type ReturnType => typeof(void);

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Join.ToString();
        }
    }
}