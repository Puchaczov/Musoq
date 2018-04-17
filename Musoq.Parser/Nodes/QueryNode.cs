using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class QueryNode : Node
    {
        public QueryNode(SelectNode select, FromNode from, JoinsNode joins, WhereNode where, GroupByNode groupBy, OrderByNode orderBy, SkipNode skip, TakeNode take)
        {
            Select = select;
            From = from;
            Where = where;
            GroupBy = groupBy;
            OrderBy = orderBy;
            Skip = skip;
            Take = take;
            Joins = joins;
            Id = $"{nameof(QueryNode)}{select.Id}{from.Id}{where?.Id}{groupBy?.Id}{orderBy?.Id}{skip?.Id}{take?.Id}";
        }

        public SelectNode Select { get; }

        public FromNode From { get; }

        public JoinsNode Joins { get; }

        public WhereNode Where { get; }

        public GroupByNode GroupBy { get; }

        public OrderByNode OrderBy { get; }

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
            var joins = Joins == null || Joins.Joins.Length == 0 ? string.Empty : Joins.Joins.Select(f => f.ToString()).Aggregate((a, b) => a + " " + b);

            return $"{Select?.ToString()} {From?.ToString()} {joins} {Where?.ToString()} {GroupBy?.ToString()} {OrderBy?.ToString()} {Skip?.ToString()} {Take?.ToString()}";
        }
    }
}