using System;

namespace Musoq.Parser.Nodes.From
{
    public class JoinSourcesTableFromNode : FromNode
    {
        internal JoinSourcesTableFromNode(FromNode first, FromNode second, Node expression, JoinType joinType)
            : base($"{first.Alias}{second.Alias}")
        {
            Id = $"{nameof(JoinSourcesTableFromNode)}{Alias}{expression.ToString()}";
            First = first;
            Second = second;
            Expression = expression;
            JoinType = joinType;
        }
        
        public JoinSourcesTableFromNode(FromNode first, FromNode second, Node expression, JoinType joinType, Type returnType)
            : base($"{first.Alias}{second.Alias}", returnType)
        {
            Id = $"{nameof(JoinSourcesTableFromNode)}{Alias}{expression.ToString()}";
            First = first;
            Second = second;
            Expression = expression;
            JoinType = joinType;
        }

        public Node Expression { get; }

        public FromNode First { get; }

        public FromNode Second { get; }

        public JoinType JoinType { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var joinType = JoinType == JoinType.Inner ? "inner join" : JoinType == JoinType.OuterLeft ? "left outer join" : "right outer join";
            
            return $"{First.ToString()} {joinType} {Second.ToString()} on {Expression.ToString()}";
        }
    }
}