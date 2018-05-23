namespace Musoq.Parser.Nodes
{
    public class DetailedQueryNode : QueryNode
    {
        public DetailedQueryNode(SelectNode select, FromNode from, WhereNode where, GroupByNode groupBy,
            OrderByNode orderBy, SkipNode skip, TakeNode take, string sourceName, string returnVariableName,
            bool mustTransformSource)
            : base(select, from, where, groupBy, orderBy, skip, take)
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