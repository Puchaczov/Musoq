using System;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Service.Visitors
{
    public class QueryCacheStringifierTraverser : IExpressionVisitor
    {
        private readonly QueryCacheStringifier _visitor;

        /// <summary>
        ///     Initialize object.
        /// </summary>
        /// <param name="visitor">Visitor that will generate code for VM</param>
        public QueryCacheStringifierTraverser(QueryCacheStringifier visitor)
        {
            _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
        }

        public void Visit(SelectNode node)
        {
            node.Accept(_visitor);
            _visitor.AddText("(");
            Iterate(node.Fields);
            _visitor.AddText(")");
        }

        public void Visit(GroupSelectNode node)
        {
        }

        public void Visit(StringNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(IntegerNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(WordNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(ContainsNode node)
        {
            node.Accept(_visitor);
            node.Right.Accept(this);
            node.Left.Accept(this);
        }

        public void Visit(AccessMethodNode node)
        {
            node.Accept(_visitor);
            node.Arguments.Accept(this);
        }

        public void Visit(IsNullNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
            node.Accept(_visitor);
            node.Arguments.Accept(this);
        }

        public void Visit(AccessColumnNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AllColumnsNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(IdentifierNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AccessObjectArrayNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AccessObjectKeyNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(PropertyValueNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(DotNode node)
        {
            node.Accept(_visitor);
            node.Expression.Accept(this);
        }

        public void Visit(AccessCallChainNode node)
        {
            node.Accept(_visitor);
        }

        /// <summary>
        ///     Visits where node in DFS manner.
        /// </summary>
        /// <param name="node">Where node that will be visited.</param>
        public virtual void Visit(WhereNode node)
        {
            node.Accept(_visitor);
            node.Expression.Accept(this);
        }

        public void Visit(GroupByNode node)
        {
            node.Accept(_visitor);
            Iterate(node.Fields);
        }

        public void Visit(HavingNode node)
        {
            node.Accept(_visitor);
            node.Expression.Accept(this);
        }

        public void Visit(SkipNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(TakeNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(JoinInMemoryWithSourceTableFromNode node)
        {
            _visitor.AddText("from");
            node.Accept(_visitor);
        }

        public void Visit(SchemaFromNode node)
        {
            _visitor.AddText("from");
            node.Accept(_visitor);
        }

        public void Visit(JoinSourcesTableFromNode node)
        {
            _visitor.AddText(node.ToString());
        }

        public void Visit(InMemoryTableFromNode node)
        {
            _visitor.AddText("from");
            node.Accept(_visitor);
        }

        public void Visit(JoinFromNode node)
        {
            throw new NotSupportedException();
        }

        public void Visit(ExpressionFromNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(CreateTableNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(RenameTableNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(TranslatedSetTreeNode node)
        {
            node.Accept(_visitor);
            foreach (var item in node.Nodes)
                item.Accept(this);
        }

        public void Visit(IntoNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(QueryScope node)
        {
            node.Accept(_visitor);
        }

        public void Visit(ShouldBePresentInTheTable node)
        {
            node.Accept(_visitor);
        }

        public void Visit(TranslatedSetOperatorNode node)
        {
            node.Accept(_visitor);
            foreach (var item in node.CreateTableNodes)
                item.Accept(_visitor);
            node.FQuery.Accept(this);
            node.SQuery.Accept(this);
        }

        public void Visit(QueryNode node)
        {
            node.Accept(_visitor);
            node.Select.Accept(this);
            node.From.Accept(this);
            node.Where.Accept(this);
            node.GroupBy?.Accept(this);
            node.GroupBy?.Having?.Accept(this);
            node.Skip?.Accept(this);
            node.Take?.Accept(this);
        }

        public void Visit(InternalQueryNode node)
        {
            node.Accept(_visitor);
            node.Select?.Accept(this);
            node.From.Accept(this);
            node.Skip?.Accept(this);
            node.Where.Accept(this);
            node.GroupBy?.Accept(this);
            node.Refresh?.Accept(this);
            node.GroupBy?.Having?.Accept(this);
            node.Take?.Accept(this);
        }

        /// <summary>
        ///     Visit OrNode node in DFS manner.
        /// </summary>
        /// <param name="node">OrNode node that will be visited.</param>
        public void Visit(OrNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(ShortCircuitingNodeLeft node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(ShortCircuitingNodeRight node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(HyphenNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        /// <summary>
        ///     Visit And node in DFS manner.
        /// </summary>
        /// <param name="node">And node that will be visited.</param>
        public void Visit(AndNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        /// <summary>
        ///     Visit Equality node in DFS manner.
        /// </summary>
        /// <param name="node">Equality node that will be visited.</param>
        public void Visit(EqualityNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(GreaterOrEqualNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(LessOrEqualNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(GreaterNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(LessNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(DiffNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(NotNode node)
        {
            node.Accept(_visitor);
            node.Expression.Accept(this);
        }

        public void Visit(LikeNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(FieldNode node)
        {
            node.Accept(_visitor);
            node.Expression.Accept(this);
        }

        /// <summary>
        ///     Visit ArgList node.
        /// </summary>
        /// <param name="node">ArgList node that will be visited.</param>
        public void Visit(ArgsListNode node)
        {
            node.Accept(_visitor);
            _visitor.AddText("(");
            Iterate(node.Args);
            _visitor.AddText(")");
        }

        /// <summary>
        ///     Visit Numeric node.
        /// </summary>
        /// <param name="node">Numeric node that will be visited.</param>
        public void Visit(DecimalNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(Node node)
        {
            throw new NotImplementedException();
        }

        public void Visit(DescNode node)
        {
            node.Accept(_visitor);
            node.From.Accept(this);
        }

        public void Visit(StarNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(FSlashNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(ModuloNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(AddNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(RootNode node)
        {
            node.Accept(_visitor);
            node.Expression.Accept(this);
        }

        public void Visit(SingleSetNode node)
        {
            node.Accept(_visitor);
            node.Query.Accept(this);
        }

        public void Visit(UnionNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(UnionAllNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(ExceptNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(RefreshNode node)
        {
            node.Accept(_visitor);
            foreach (var item in node.Nodes)
                item.Accept(this);
        }

        public void Visit(IntersectNode node)
        {
            node.Left.Accept(this);
            node.Accept(_visitor);
            node.Right.Accept(this);
        }

        public void Visit(PutTrueNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(MultiStatementNode node)
        {
            node.Accept(_visitor);
            foreach (var cNode in node.Nodes)
                cNode.Accept(this);
        }

        public void Visit(CteExpressionNode node)
        {
            node.Accept(_visitor);
            Iterate(node.InnerExpression);
            node.OuterExpression.Accept(this);
        }

        public void Visit(CteInnerExpressionNode node)
        {
            node.Accept(_visitor);
            _visitor.AddText("(");
            node.Value.Accept(this);
            _visitor.AddText(")");
        }

        public void Visit(JoinsNode node)
        {
            node.Joins.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(JoinNode node)
        {
            node.From.Accept(this);
            node.Accept(_visitor);
            node.Expression.Accept(this);
        }

        private void Iterate<TField>(TField[] arr)
            where TField : Node
        {
            if (arr.Length == 0)
                return;

            for (var i = 0; i < arr.Length - 1; i++)
            {
                arr[i].Accept(this);
                _visitor.AddText(",");
            }

            arr[arr.Length - 1].Accept(this);
        }

        public void Visit(FromNode node)
        {
            node.Accept(_visitor);
        }

        /// <summary>
        ///     Visit Function node in DFS manner.
        /// </summary>
        /// <param name="node">Function node that will be visited.</param>
        public virtual void Visit(RawFunctionNode node)
        {
            node.Accept(_visitor);
            foreach (var item in node.Parameters.Reverse())
                item.Accept(this);
        }
    }
}