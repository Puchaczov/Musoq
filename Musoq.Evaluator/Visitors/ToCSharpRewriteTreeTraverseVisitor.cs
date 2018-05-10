using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Evaluator.RuntimeScripts;
using Musoq.Evaluator.Utils;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using IdentifierNode = Musoq.Parser.Nodes.IdentifierNode;

namespace Musoq.Evaluator.Visitors
{
    public class ToCSharpRewriteTreeTraverseVisitor : IExpressionVisitor
    {
        private readonly IToCSharpTranslationExpressionVisitor _visitor;
        private ScopeWalker _walker;
        private readonly StringBuilder _code = new StringBuilder();
        private bool _hasGroupBy;
        private bool _hasJoin;

        public ToCSharpRewriteTreeTraverseVisitor(IToCSharpTranslationExpressionVisitor visitor, ISchemaProvider provider, ScopeWalker walker)
        {
            _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
            _walker = walker;
        }

        public void Visit(SelectNode node)
        {
            _visitor.TurnOnAggregateMethodsToColumnAcceess();

            _code.Append(new Select().TransformText());
            foreach (var field in node.Fields)
                field.Accept(this);
            node.Accept(_visitor);


            _visitor.TurnOffAggregateMethodsToColumnAcceess();
        }

        public void Visit(GroupSelectNode node)
        {
            _code.Append(new Select().TransformText());
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
            node.Left.Accept(this);
            node.Right.Accept(this);
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

        public void Visit(IdentifierNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AccessObjectArrayNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AccessObjectKeyNode node)
        {}

        public void Visit(PropertyValueNode node)
        {}

        public void Visit(DotNode node)
        {
            node.Root.Accept(this);
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(AccessCallChainNode node)
        {
            node.Accept(_visitor);
        }

        public virtual void Visit(WhereNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GroupByNode node)
        {
            foreach (var field in node.Fields)
                field.Accept(this);

            node.Having?.Accept(this);
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

        public void Visit(JoinInMemoryWithSourceTableFromNode node)
        {
            node.SourceTable.Accept(this);
            node.Expression.Accept(this);
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

        public void Visit(InMemoryTableFromNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(JoinFromNode node)
        {
            var joins = new Stack<JoinFromNode>();

            var join = node;
            while (join != null)
            {
                joins.Push(join);
                join = join.Source as JoinFromNode;
            }

            _visitor.SetJoinsAmount(joins.Count + 1);

            var nestedForeachesPattern = new NestedForeaches
            {
                Nesting = joins.Count + 1,
                HasGroupBy = _hasGroupBy
            };

            _code.Append(nestedForeachesPattern.TransformText());

            join = joins.Pop();

            join.Source.Accept(this);
            join.With.Accept(this);
            join.Expression.Accept(this);

            while (joins.Count > 1)
            {
                join = joins.Pop();
                join.With.Accept(this);
                join.Expression.Accept(this);
            }

            if (joins.Count > 0)
            {
                join = joins.Pop();
                join.With.Accept(this);
                join.Expression.Accept(this);
            }

            join.Accept(_visitor);

            _hasJoin = true;
        }

        public void Visit(ExpressionFromNode node)
        {
            _visitor.SetQueryIdentifier(node.Alias);
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(CreateTableNode node)
        {
            _walker = _walker.NextChild();
            _visitor.SetScope(_walker.Scope);

            node.Accept(_visitor);

            _walker = _walker.Parent();
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
            foreach (var item in node.CreateTableNodes)
                item.Accept(_visitor);

            node.FQuery.Accept(this);
            node.SQuery.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(QueryNode node)
        {
            _walker = _walker.NextChild();
            _visitor.SetScope(_walker.Scope);

            _visitor.SetMethodAccessType(MethodAccessType.ResultQuery);
            _visitor.SetQueryIdentifier(node.From.Alias);
            _visitor.QueryBegins();

            node.From.Accept(this);
            node.Where.Accept(this);
            node.Select.Accept(this);

            node.Take?.Accept(this);
            node.Skip?.Accept(this);
            node.GroupBy?.Accept(this);
            node.Accept(_visitor);

            _visitor.QueryEnds();
            _walker = _walker.Parent();
        }

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

        public void Visit(AndNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

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

        public void Visit(ArgsListNode node)
        {
            foreach (var item in node.Args)
                item.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(DecimalNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(Node node)
        {
            throw new NotSupportedException();
        }

        public void Visit(DescNode node)
        {
            _walker = _walker.NextChild();

            node.From.Accept(this);
            node.Accept(_visitor);

            _walker = _walker.Parent();
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

        public void Visit(InternalQueryNode node)
        {
            _walker = _walker.NextChild();
            _visitor.SetScope(_walker.Scope);

            _visitor.SetMethodAccessType(MethodAccessType.TransformingQuery);
            _visitor.SetQueryIdentifier(node.From.Alias);
            node.Refresh?.Accept(this);
            node.From.Accept(this);
            node.Where.Accept(this);
            node.Select.Accept(this);
            node.Take?.Accept(this);
            node.Skip?.Accept(this);
            node.GroupBy?.Accept(this);
            node.Accept(_visitor);
            
            _walker = _walker.Parent();
        }

        public void Visit(RootNode node)
        {
            _visitor.SetCodePattern(_code);
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(SingleSetNode node)
        {
            node.Query.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(UnionNode node)
        {
            TraverseSetOperator(node);
        }

        public void Visit(UnionAllNode node)
        {
            TraverseSetOperator(node);
        }

        public void Visit(ExceptNode node)
        {
            TraverseSetOperator(node);
        }

        public void Visit(RefreshNode node)
        {
            foreach (var item in node.Nodes)
                item.Accept(this);

            node.Accept(_visitor);
        }

        public void Visit(IntersectNode node)
        {
            TraverseSetOperator(node);
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
            {
                exp.Accept(this);
            }
            node.OuterExpression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(CteInnerExpressionNode node)
        {
            node.Value.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(JoinsNode node)
        {
            node.Joins.Accept(this);
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

        private void TraverseSetOperator(SetOperatorNode node)
        {
            if (node.Right is SetOperatorNode)
            {
                var nodes = new Stack<SetOperatorNode>();
                nodes.Push(node);

                node.Left.Accept(this);

                while (nodes.Count > 0)
                {
                    var current = nodes.Pop();

                    if (current.Right is SetOperatorNode operatorNode)
                    {
                        nodes.Push(operatorNode);
                        
                        operatorNode.Left.Accept(this);
                        current.Accept(_visitor);
                    }
                    else
                    {
                        current.Right.Accept(this);
                        current.Accept(_visitor);
                    }
                }
            }
            else
            {
                node.Left.Accept(this);
                node.Right.Accept(this);
                node.Accept(_visitor);
            }
        }
    }
}