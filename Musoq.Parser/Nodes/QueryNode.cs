using System;
using System.Text;

namespace Musoq.Parser.Nodes
{
    public class QueryNode : Node
    {
        public QueryNode(SelectNode select, FromNode from, WhereNode where, GroupByNode groupBy, OrderByNode orderBy,
            SkipNode skip, TakeNode take)
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

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            
            if (Select != null)
                builder.Append(Select.ToString());
            
            if (From != null)
                builder.Append(From.ToString());
            
            if (Where != null)
                builder.Append(Where.ToString());
            
            if (GroupBy != null)
                builder.Append(GroupBy.ToString());
            
            if (OrderBy != null)
                builder.Append(OrderBy.ToString());
            
            if (Skip != null)
                builder.Append(Skip.ToString());
            
            if (Take != null)
                builder.Append(Take.ToString());
            
            return builder.ToString();
        }
    }
}