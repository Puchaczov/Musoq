using System;

namespace Musoq.Parser.Nodes.From
{
    public class JoinFromNode : FromNode
    {
        internal JoinFromNode(FromNode source, FromNode with, Node expression, JoinType joinType)
            : base($"{source.Alias}{with.Alias}")
        {
            Source = source;
            With = with;
            Expression = expression;
            JoinType = joinType;
        }
        
        public JoinFromNode(FromNode source, FromNode with, Node expression, JoinType joinType, Type returnType)
            : base($"{source.Alias}{with.Alias}", returnType)
        {
            Source = source;
            With = with;
            Expression = expression;
            JoinType = joinType;
        }

        public FromNode Source { get; }
        public FromNode With { get; }
        public Node Expression { get; }
        public JoinType JoinType { get; }
        public override string Id => $"{typeof(JoinFromNode)}{Source.Id}{With.Id}{Expression.Id}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var joinType = JoinType == JoinType.Inner ? "inner join" : JoinType == JoinType.OuterLeft ? "left outer join" : "right outer join";
            
            return $"{Source.ToString()} {joinType} {With.ToString()} on {Expression.ToString()}";
        }
    }
}