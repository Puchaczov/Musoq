using System;
using System.Text;

namespace Musoq.Parser.Nodes;

public class QueryNode : Node
{
    public QueryNode(SelectNode select, FromNode from, WhereNode where, GroupByNode groupBy, OrderByNode orderBy,
        SkipNode skip, TakeNode take)
        : this(select, from, where, groupBy, orderBy, skip, take, default)
    {
    }

    public QueryNode(SelectNode select, FromNode from, WhereNode where, GroupByNode groupBy, OrderByNode orderBy,
        SkipNode skip, TakeNode take, TextSpan span)
    {
        Select = select;
        From = from;
        Where = where;
        GroupBy = groupBy;
        OrderBy = orderBy;
        Skip = skip;
        Take = take;
        Id = $"{nameof(QueryNode)}{select.Id}{from.Id}{where?.Id}{groupBy?.Id}{orderBy?.Id}{skip?.Id}{take?.Id}";

        // Compute span from first to last clause
        if (span.IsEmpty)
        {
            var nodes = new Node[] { select, from, where, groupBy, orderBy, skip, take };
            Span = ComputeSpan(nodes);
            FullSpan = Span;
        }
        else
        {
            Span = span;
            FullSpan = span;
        }
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

        var otherClauses = new Node[]
        {
            From,
            Where,
            GroupBy,
            OrderBy,
            Skip
        };

        builder.Append(Select.ToString());

        const char separator = ' ';

        foreach (var clause in otherClauses)
        {
            if (clause == null)
                continue;

            var clauseString = clause.ToString();

            if (clauseString is { Length: > 0 } && builder[^1] != separator && clauseString[0] != separator)
                builder.Append(separator);

            builder.Append(clauseString);
        }

        if (Take == null) return builder.ToString();

        var takeString = Take.ToString();

        if (takeString is { Length: > 0 } && builder[^1] != separator && takeString[0] != separator)
            builder.Append(separator);

        builder.Append(takeString);

        return builder.ToString();
    }
}
