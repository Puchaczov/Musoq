using System;

namespace Musoq.Parser.Nodes
{
    public class QueryNode : Node
    {
        public QueryNode(SelectNode select, FromNode from, WhereNode where, GroupByNode groupBy, OrderByNode orderBy, SkipNode skip, TakeNode take)
        {
            Select = select;
            From = from;
            Where = where;
            GroupBy = groupBy;
            OrderBy = orderBy;
            Skip = skip;
            Take = take;
            Id = $"{nameof(QueryNode)}{select.Id}{from.Id}{where?.Id}{groupBy?.Id}{orderBy?.Id}{skip?.Id}{take?.Id}";
        }

        public SelectNode Select { get; }

        public FromNode From { get; }

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

            return $"{Select?.ToString()} {From?.ToString()} {Where?.ToString()} {GroupBy?.ToString()} {OrderBy?.ToString()} {Skip?.ToString()} {Take?.ToString()}";
        }
    }

    public class DetailedQueryNode : QueryNode
    {
        public DetailedQueryNode(SelectNode @select, FromNode @from, WhereNode @where, GroupByNode groupBy, OrderByNode orderBy, SkipNode skip, TakeNode take, string sourceName, string returnVariableName, bool mustTransformSource) 
            : base(@select, @from, @where, groupBy, orderBy, skip, take)
        {
            SourceName = sourceName;
            MustTransformSource = mustTransformSource;
            ReturnVariableName = returnVariableName;
        }

        public string SourceName { get; }

        public string ReturnVariableName { get; }

        public bool MustTransformSource { get; }
    }
}