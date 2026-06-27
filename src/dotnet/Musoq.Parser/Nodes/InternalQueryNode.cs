namespace Musoq.Parser.Nodes;

public class InternalQueryNode(
    SelectNode select,
    FromNode from,
    WhereNode? where,
    GroupByNode? groupBy,
    OrderByNode? orderBy,
    SkipNode? skip,
    TakeNode? take,
    RefreshNode refresh)
    : QueryNode(select, from, where, groupBy, orderBy, skip, take)
{
    public RefreshNode Refresh { get; } = refresh;

    public override Type? ReturnType => null;

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return
            $"{Select.ToString()} {From.ToString()} {Where?.ToString()} {GroupBy?.ToString()} {OrderBy?.ToString()} {Skip?.ToString()} {Take?.ToString()}";
    }
}
