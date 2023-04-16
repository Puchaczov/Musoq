using System;

namespace Musoq.Parser.Nodes.From
{
    public class JoinsNode : FromNode
    {
        internal JoinsNode(JoinFromNode joins)
            : base(joins.Alias)
        {
            Id = $"{nameof(JoinsNode)}{joins.Id}";
            Joins = joins;
        }
        
        public JoinsNode(JoinFromNode joins, Type returnType)
            : base(joins.Alias, returnType)
        {
            Id = $"{nameof(JoinsNode)}{joins.Id}";
            Joins = joins;
        }

        public JoinFromNode Joins { get; }

        public override Type ReturnType => typeof(void);

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Joins.ToString();
        }
    }
}