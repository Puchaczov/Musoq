using System;

namespace Musoq.Parser.Nodes
{
    public class QueryNode : Node
    {
        public QueryNode(SelectNode select, FromNode from, WhereNode where, GroupByNode groupBy, SkipNode skip, TakeNode take)
        {
            Select = select;
            From = from;
            Where = where;
            GroupBy = groupBy;
            Skip = skip;
            Take = take;
            Id = $"{nameof(QueryNode)}{select.Id}{from.Id}{where?.Id}{groupBy?.Id}{skip?.Id}{take?.Id}";
        }

        public SelectNode Select { get; }

        public FromNode From { get; }

        public WhereNode Where { get; }

        public GroupByNode GroupBy { get; }

        public SkipNode Skip { get; }

        public TakeNode Take { get; }

        public override Type ReturnType => null;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"{Select?.ToString()} {From?.ToString()} {Where?.ToString()} {GroupBy?.ToString()} {Skip?.ToString()} {Take?.ToString()}";
        }
    }
}