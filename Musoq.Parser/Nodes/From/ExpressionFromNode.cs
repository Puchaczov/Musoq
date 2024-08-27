using System;

namespace Musoq.Parser.Nodes.From
{
    public class ExpressionFromNode : FromNode
    {
        internal ExpressionFromNode(FromNode from)
            : base(from.Alias)
        {
            Expression = from;
            Id = $"{nameof(ExpressionFromNode)}{from.ToString()}";
        }
        
        public ExpressionFromNode(FromNode from, Type returnType)
            : base(from.Alias, returnType)
        {
            Expression = from;
            Id = $"{nameof(ExpressionFromNode)}{from.ToString()}";
        }

        public FromNode Expression { get; }

        public override string Alias => Expression.Alias;

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }
}