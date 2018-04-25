using System;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class CodeGenerationTraverseVisitor : IExpressionVisitor
    {
        private readonly IExpressionVisitor _visitor;

        /// <summary>
        ///     Initialize object.
        /// </summary>
        /// <param name="visitor">Visitor that will generate code for VM</param>
        public CodeGenerationTraverseVisitor(IExpressionVisitor visitor)
        {
            _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
        }

        public void Visit(SelectNode node)
        {
            foreach (var field in node.Fields)
                field.Accept(this);
            node.Accept(_visitor);
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
            node.Right.Accept(this);
            node.Left.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(AccessMethodNode node)
        {
            node.Arguments.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GroupByAccessMethodNode node)
        {
            node.Arguments.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
            node.Arguments.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(AccessColumnNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AllColumnsNode node)
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
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GroupByNode node)
        {
            foreach (var field in node.Fields)
                field.Accept(this);

            node.Accept(_visitor);
        }

        public void Visit(HavingNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(SkipNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(TakeNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(ExistingTableFromNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(SchemaFromNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(NestedQueryFromNode node)
        {
            node.Query.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(CteFromNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(JoinFromNode node)
        {
            while (node.Source is JoinFromNode)
            {
                node.Source.Accept(this);
            }
            node.Accept(_visitor);
            node.Expression.Accept(this);
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
            foreach (var item in node.Nodes)
                item.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(IntoNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(IntoGroupNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(ShouldBePresentInTheTable node)
        {
            node.Accept(_visitor);
        }

        public void Visit(TranslatedSetOperatorNode node)
        {
            foreach (var item in node.CreateTableNodes)
                item.Accept(_visitor);
            node.FQuery.Accept(this);
            node.SQuery.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(QueryNode node)
        {
            node.From.Accept(this);
            node.Skip?.Accept(this);
            node.Where.Accept(this);
            node.GroupBy?.Accept(this);
            node.GroupBy?.Having?.Accept(this);
            node.Take?.Accept(this);
            node.Select.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(InternalQueryNode node)
        {
            node.From.Accept(this);
            node.Skip?.Accept(this);
            node.Where.Accept(this);
            node.GroupBy?.Accept(this);
            node.Refresh?.Accept(this);
            node.GroupBy?.Having?.Accept(this);
            node.Take?.Accept(this);
            node.Select?.Accept(this);
            node.ShouldBePresent?.Accept(_visitor);
            node.Into?.Accept(_visitor);
            node.Accept(_visitor);
        }

        /// <summary>
        ///     Visit OrNode node in DFS manner.
        /// </summary>
        /// <param name="node">OrNode node that will be visited.</param>
        public void Visit(OrNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
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
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        /// <summary>
        ///     Visit And node in DFS manner.
        /// </summary>
        /// <param name="node">And node that will be visited.</param>
        public void Visit(AndNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        /// <summary>
        ///     Visit Equality node in DFS manner.
        /// </summary>
        /// <param name="node">Equality node that will be visited.</param>
        public void Visit(EqualityNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GreaterOrEqualNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(LessOrEqualNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GreaterNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(LessNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(DiffNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(NotNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(LikeNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(FieldNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        /// <summary>
        ///     Visit ArgList node.
        /// </summary>
        /// <param name="node">ArgList node that will be visited.</param>
        public void Visit(ArgsListNode node)
        {
            foreach (var item in node.Args)
                item.Accept(this);
            node.Accept(_visitor);
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
        }

        public void Visit(StarNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(FSlashNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(ModuloNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(AddNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(RootNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(SingleSetNode node)
        {
            node.Accept(_visitor);
            node.Query.Accept(this);
        }

        public void Visit(UnionNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(UnionAllNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(ExceptNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(RefreshNode node)
        {
            foreach (var item in node.Nodes)
                item.Accept(this);

            node.Accept(_visitor);
        }

        public void Visit(IntersectNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(PutTrueNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(MultiStatementNode node)
        {
            foreach (var cNode in node.Nodes)
                cNode.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(CteExpressionNode node)
        {
            foreach (var exp in node.InnerExpression)
                exp.Accept(this);
            node.Accept(_visitor);
            node.OuterExpression.Accept(this);
        }

        public void Visit(CteInnerExpressionNode node)
        {
            node.Value.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(JoinsNode node)
        {
            foreach (var item in node.Joins)
                item.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(JoinNode node)
        {
            node.From.Accept(this);
            node.Expression.Accept(this);
            node.Accept(_visitor);
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
            foreach (var item in node.Parameters.Reverse())
                item.Accept(this);
            node.Accept(_visitor);
        }
    }
}