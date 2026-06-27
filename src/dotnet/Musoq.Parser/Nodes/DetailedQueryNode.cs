namespace Musoq.Parser.Nodes;

public class DetailedQueryNode(
    SelectNode select,
    FromNode from,
    WhereNode? where,
    GroupByNode? groupBy,
    OrderByNode? orderBy,
    SkipNode? skip,
    TakeNode? take,
    string returnVariableName,
    WindowNode? window = null,
    QualifyNode? qualify = null)
    : QueryNode(select, from, where, groupBy, orderBy, skip, take, window, qualify, default)
{
    public string ReturnVariableName { get; } = returnVariableName;
}
