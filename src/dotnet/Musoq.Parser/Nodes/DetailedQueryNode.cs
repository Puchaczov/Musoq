namespace Musoq.Parser.Nodes;

public class DetailedQueryNode : QueryNode
{
    public DetailedQueryNode(SelectNode select, FromNode from, WhereNode where, GroupByNode groupBy,
        OrderByNode orderBy, SkipNode skip, TakeNode take, string returnVariableName,
        WindowNode window = null)
        : base(select, from, where, groupBy, orderBy, skip, take, window)
    {
        ReturnVariableName = returnVariableName;
    }

    public string ReturnVariableName { get; }
}
