using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors
{
    public class BuildMetadataAndInferTypeTraverseVisitor : IExpressionVisitor
    {
        private readonly ISchemaProvider _provider;
        private readonly IScopeAwareExpressionVisitor _visitor;
        private readonly Stack<Scope> _scopes = new Stack<Scope>();
        private Scope _current = new Scope(null, -1);
        public Scope Scope => _current;

        public BuildMetadataAndInferTypeTraverseVisitor(IScopeAwareExpressionVisitor visitor, ISchemaProvider provider)
        {
            _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
            _provider = provider;
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

            var ident = (IdentifierNode) self.Root;
            if (_current.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(ident.Name))
            {
                if (self.Expression is DotNode dotNode)
                {
                    var col = (IdentifierNode) dotNode.Root;
                    Visit(new AccessColumnNode(col.Name, ident.Name, TextSpan.Empty));
                }
                else
                {
                    var col = (IdentifierNode) self.Expression;
                    Visit(new AccessColumnNode(col.Name, ident.Name, TextSpan.Empty));
                }

                self = self.Expression as DotNode;
            }


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

        public void Visit(ExistingTableFromNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(SchemaFromNode node)
        {
            var schema = _provider.GetSchema(node.Schema);
            var table = schema.GetTableByName(node.Method, node.Parameters);
            
            _current.ScopeSymbolTable.AddSymbol(node.Alias, new TableSymbol(node.Alias, schema, table));

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

            join = joins.Pop();
            join.Source.Accept(this);
            join.With.Accept(this);

            var firstTableSymbol = _current.ScopeSymbolTable.GetSymbol<TableSymbol>(join.Source.Alias);
            var secondTableSymbol = _current.ScopeSymbolTable.GetSymbol<TableSymbol>(join.With.Alias);

            var id = $"{join.Source.Alias}{join.With.Alias}";

            _current.ScopeSymbolTable.AddSymbol(id, firstTableSymbol.MergeSymbols(secondTableSymbol));

            _visitor.SetQueryIdentifier(id);
            join.Expression.Accept(this);
            join.Accept(_visitor);

            while (joins.Count > 0)
            {
                join = joins.Pop();
                join.With.Accept(this);

                var currentTableSymbol = _current.ScopeSymbolTable.GetSymbol<TableSymbol>(join.With.Alias);
                var previousTableSymbol = _current.ScopeSymbolTable.GetSymbol<TableSymbol>(id);

                id = $"{id}{join.With.Alias}";

                _current.ScopeSymbolTable.AddSymbol(id, previousTableSymbol.MergeSymbols(currentTableSymbol));

                _visitor.SetQueryIdentifier(id);
                join.Expression.Accept(this);
                join.Accept(_visitor);
            }
        }

        public void Visit(ExpressionFromNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(CreateTableNode node)
        {
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
            LoadScope();

            _visitor.SetQueryIdentifier(node.From.Alias);
            _visitor.QueryBegins();
            node.From.Accept(this);

            node.Joins.Accept(this);
            node.Where.Accept(this);
            node.Select.Accept(this);
            node.Take?.Accept(this);
            node.Skip?.Accept(this);
            node.GroupBy?.Accept(this);
            node.Accept(_visitor);

            _visitor.QueryEnds();

            RestoreScope();
        }

        private void LoadScope()
        {
            var newScope = _current.AddScope();
            _scopes.Push(_current);
            _current = newScope;

            _visitor.SetScope(newScope);
        }

        private void RestoreScope()
        {
            _current = _scopes.Pop();
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

            node.From.Accept(this);
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