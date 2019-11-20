using System;
using System.Collections.Generic;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class BuildMetadataAndInferTypeTraverseVisitor : IQueryPartAwareExpressionVisitor
    {
        private readonly Stack<Scope> _scopes = new Stack<Scope>();
        private readonly IAwareExpressionVisitor _visitor;

        public BuildMetadataAndInferTypeTraverseVisitor(IAwareExpressionVisitor visitor)
        {
            _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
        }

        public Scope Scope { get; private set; } = new Scope(null, -1, "Root");

        public void Visit(SelectNode node)
        {
            SetQueryPart(QueryPart.Select);
            foreach (var field in node.Fields)
                field.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GroupSelectNode node)
        {
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

        public void Visit(BooleanNode node)
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

        public void Visit(AccessRawIdentifierNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(IsNullNode node)
        {
            node.Expression.Accept(this);
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
        {
            node.Accept(_visitor);
        }

        public void Visit(PropertyValueNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(DotNode node)
        {
            var self = node;

            var theMostInner = self;
            while (!(self is null))
            {
                theMostInner = self;
                self = self.Root as DotNode;
            }

            var ident = (IdentifierNode) theMostInner.Root;
            if (node == theMostInner && Scope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(ident.Name))
            {
                if (theMostInner.Expression is DotNode dotNode)
                {
                    var col = (IdentifierNode) dotNode.Root;
                    Visit(new AccessColumnNode(col.Name, ident.Name, TextSpan.Empty));
                }
                else
                {
                    var col = (IdentifierNode) theMostInner.Expression;
                    Visit(new AccessColumnNode(col.Name, ident.Name, TextSpan.Empty));
                }

                return;
            }

            self = node;

            while (!(self is null))
            {
                self.Root.Accept(this);
                self.Expression.Accept(this);
                self.Accept(_visitor);

                self = self.Expression as DotNode;
            }
        }

        public void Visit(AccessCallChainNode node)
        {
            node.Accept(_visitor);
        }

        public virtual void Visit(WhereNode node)
        {
            SetQueryPart(QueryPart.Where);
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GroupByNode node)
        {
            SetQueryPart(QueryPart.GroupBy);

            foreach (var field in node.Fields)
                field.Accept(this);

            node.Having?.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(HavingNode node)
        {
            SetQueryPart(QueryPart.Having);
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
            SetQueryPart(QueryPart.From);
            node.SourceTable.Accept(this);
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(SchemaFromNode node)
        {
            SetQueryPart(QueryPart.From);
            node.Parameters.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(JoinSourcesTableFromNode node)
        {
            SetQueryPart(QueryPart.From);
            node.Expression.Accept(this);
            node.First.Accept(this);
            node.Second.Accept(this);

            node.Accept(_visitor);
        }

        public void Visit(InMemoryTableFromNode node)
        {
            SetQueryPart(QueryPart.From);
            node.Accept(_visitor);
        }

        public void Visit(JoinFromNode node)
        {
            SetQueryPart(QueryPart.From);
            var joins = new Stack<JoinFromNode>();

            var join = node;
            while (join != null)
            {
                joins.Push(join);
                join = join.Source as JoinFromNode;
            }

            join = joins.Pop();
            join.Source.Accept(this);
            join.With.Accept(this);

            var firstTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[join.Source.Id]);
            var secondTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[join.With.Id]);

            switch (join.JoinType)
            {
                case JoinType.Inner:
                    break;
                case JoinType.OuterLeft:
                    secondTableSymbol = secondTableSymbol.MakeNullableIfPossible();
                    Scope.ScopeSymbolTable.UpdateSymbol(Scope[join.With.Id], secondTableSymbol);
                    break;
                case JoinType.OuterRight:
                    firstTableSymbol = firstTableSymbol.MakeNullableIfPossible();
                    Scope.ScopeSymbolTable.UpdateSymbol(Scope[join.Source.Id], firstTableSymbol);
                    break;
            }

            var id = $"{Scope[join.Source.Id]}{Scope[join.With.Id]}";

            Scope.ScopeSymbolTable.AddSymbol(id, firstTableSymbol.MergeSymbols(secondTableSymbol));
            Scope[MetaAttributes.ProcessedQueryId] = id;

            join.Expression.Accept(this);
            join.Accept(_visitor);

            while (joins.Count > 0)
            {
                join = joins.Pop();
                join.With.Accept(this);

                var currentTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[join.With.Id]);
                var previousTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(id);

                switch (join.JoinType)
                {
                    case JoinType.Inner:
                        break;
                    case JoinType.OuterLeft:
                        currentTableSymbol = currentTableSymbol.MakeNullableIfPossible();
                        Scope.ScopeSymbolTable.UpdateSymbol(Scope[join.With.Id], currentTableSymbol);
                        break;
                    case JoinType.OuterRight:
                        previousTableSymbol = previousTableSymbol.MakeNullableIfPossible();
                        Scope.ScopeSymbolTable.UpdateSymbol(id, previousTableSymbol);
                        break;
                }

                id = $"{id}{Scope[join.With.Id]}";

                Scope.ScopeSymbolTable.AddSymbol(id, previousTableSymbol.MergeSymbols(currentTableSymbol));
                Scope[MetaAttributes.ProcessedQueryId] = id;

                join.Expression.Accept(this);
                join.Accept(_visitor);
            }
        }

        public void Visit(ExpressionFromNode node)
        {
            SetQueryPart(QueryPart.From);
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(CreateTransformationTableNode node)
        {
            SetQueryPart(QueryPart.None);
            foreach (var item in node.Fields)
                item.Accept(this);

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
            LoadScope("Query");
            node.From.Accept(this);
            node.Where?.Accept(this);
            node.GroupBy?.Accept(this);
            node.Select.Accept(this);
            node.Skip?.Accept(this);
            node.Take?.Accept(this);
            node.OrderBy?.Accept(this);
            node.Accept(_visitor);
            RestoreScope();
            SetQueryPart(QueryPart.None);
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

        public void Visit(RLikeNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(InNode node)
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

        public void Visit(FieldOrderedNode node)
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
            throw new NotSupportedException("Node cannot be visited.");
        }

        public void Visit(DescNode node)
        {
            LoadScope("Desc");
            node.From.Accept(this);
            node.Accept(_visitor);
            RestoreScope();
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
        }

        public void Visit(RootNode node)
        {
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
            LoadScope("Union");
            TraverseSetOperator(node);
        }

        public void Visit(UnionAllNode node)
        {
            LoadScope("UnionAll");
            TraverseSetOperator(node);
        }

        public void Visit(ExceptNode node)
        {
            LoadScope("Except");
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
            LoadScope("Intersect");
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
            LoadScope("CTE");
            foreach (var exp in node.InnerExpression) exp.Accept(this);

            node.OuterExpression.Accept(this);
            node.Accept(_visitor);
            RestoreScope();
        }

        public void Visit(CteInnerExpressionNode node)
        {
            LoadScope("CTE Inner Expression");
            node.Value.Accept(this);
            node.Accept(_visitor);
            RestoreScope();
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

        private void LoadScope(string name)
        {
            var newScope = Scope.AddScope(name);
            _scopes.Push(Scope);
            Scope = newScope;

            _visitor.SetScope(newScope);
        }

        private void RestoreScope()
        {
            Scope = _scopes.Pop();
            _visitor.SetScope(Scope);
        }

        public void Visit(FromNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(OrderByNode node)
        {
            foreach (var field in node.Fields)
                field.Accept(this);

            node.Accept(_visitor);
        }

        private void TraverseSetOperator(SetOperatorNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
            RestoreScope();
        }

        public void SetQueryPart(QueryPart part)
        {
            _visitor.SetQueryPart(part);
        }

        public void Visit(CreateTableNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(CoupleNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(SchemaMethodFromNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AliasedFromNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(StatementsArrayNode node)
        {
            foreach (var statement in node.Statements)
                statement.Accept(this);

            node.Accept(_visitor);
        }

        public void Visit(StatementNode node)
        {
            node.Node.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(CaseNode node)
        {
            node.Else.Accept(this);
            
            for(int i = node.WhenThenPairs.Length - 1; i >= 0; --i)
            {
                node.WhenThenPairs[i].When.Accept(this);
                node.WhenThenPairs[i].Then.Accept(this);
            }

            node.Accept(_visitor);
        }

        public void Visit(FieldLinkNode node)
        {
            node.Accept(_visitor);
        }
    }
}