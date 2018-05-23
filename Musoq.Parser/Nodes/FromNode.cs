using System;
using Musoq.Schema.DataSources;

namespace Musoq.Parser.Nodes
{
    public abstract class FromNode : Node
    {
        protected FromNode(string alias)
        {
            Alias = alias;
        }

        public virtual string Alias { get; }

        public override Type ReturnType => typeof(RowSource);
    }

    public class InMemoryGroupedFromNode : FromNode
    {
        public InMemoryGroupedFromNode(string alias) 
            : base(alias)
        {
            Id = $"{nameof(InMemoryTableFromNode)}{Alias}";
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
        public override string ToString()
        {
            return Alias;
        }
    }

    public enum JoinType
    {
        Inner,
        OuterLeft,
        OuterRight
    }

    public class JoinFromNode : FromNode
    {
        public FromNode Source { get; }
        public FromNode With { get; }
        public Node Expression { get; }
        public JoinType JoinType { get; }
        public JoinFromNode(FromNode joinFrom, FromNode from, Node expression, JoinType joinType) 
            : base(string.Empty)
        {
            Source = joinFrom;
            With = @from;
            Expression = expression;
            JoinType = joinType;
        }

        public override string Alias => $"{Source.Alias}{With.Alias}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id => $"{typeof(JoinFromNode)}{Source.Id}{With.Id}{Expression.Id}";

        public override string ToString()
        {
            return $"({Source.ToString()}, {With.ToString()}, {Expression.ToString()})";
        }
    }

    public class JoinExpressionFromNode : ExpressionFromNode
    {
        public JoinExpressionFromNode(FromNode @from) : base(@from)
        {
        }
    }

    public class ExpressionFromNode : FromNode
    {
        public ExpressionFromNode(FromNode @from)
            : base(from.Alias)
        {
            Expression = from;
            Id = $"{nameof(ExpressionFromNode)}{from.ToString()}";
        }

        public FromNode Expression { get; }

        public override string Alias => Expression.Alias;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
        public override string ToString()
        {
            return $"from ({Expression.ToString()})";
        }
    }
}