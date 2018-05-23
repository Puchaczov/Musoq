namespace Musoq.Parser.Nodes
{
    public class DetailedQueryNode : QueryNode
    {
        public DetailedQueryNode(SelectNode select, FromNode from, WhereNode where, GroupByNode groupBy,
            OrderByNode orderBy, SkipNode skip, TakeNode take, string returnVariableName)
            : base(select, from, where, groupBy, orderBy, skip, take)
        {
            ReturnVariableName = returnVariableName;
        }

        public string ReturnVariableName { get; }
    }
}