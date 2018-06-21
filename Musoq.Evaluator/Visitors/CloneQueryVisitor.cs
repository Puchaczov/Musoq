using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class CloneQueryVisitor : IExpressionVisitor
    {
        protected Stack<Node> Nodes { get; } = new Stack<Node>();

        public RootNode Root => (RootNode) Nodes.Peek();

        public void Visit(Node node)
        {
        }

        public virtual void Visit(DescNode node)
        {
            Nodes.Push(new DescNode((FromNode) Nodes.Pop()));
        }

        public virtual void Visit(StarNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new StarNode(left, right));
        }

        public virtual void Visit(FSlashNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new FSlashNode(left, right));
        }

        public virtual void Visit(ModuloNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new ModuloNode(left, right));
        }

        public virtual void Visit(AddNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new AddNode(left, right));
        }

        public virtual void Visit(HyphenNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new HyphenNode(left, right));
        }

        public virtual void Visit(AndNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new AndNode(left, right));
        }

        public virtual void Visit(OrNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new OrNode(left, right));
        }

        public virtual void Visit(ShortCircuitingNodeLeft node)
        {
            Nodes.Push(new ShortCircuitingNodeLeft(Nodes.Pop(), node.UsedFor));
        }

        public virtual void Visit(ShortCircuitingNodeRight node)
        {
            Nodes.Push(new ShortCircuitingNodeRight(Nodes.Pop(), node.UsedFor));
        }

        public virtual void Visit(EqualityNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new EqualityNode(left, right));
        }

        public virtual void Visit(GreaterOrEqualNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new GreaterOrEqualNode(left, right));
        }

        public virtual void Visit(LessOrEqualNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LessOrEqualNode(left, right));
        }

        public virtual void Visit(GreaterNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new GreaterNode(left, right));
        }

        public virtual void Visit(LessNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LessNode(left, right));
        }

        public virtual void Visit(DiffNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new DiffNode(left, right));
        }

        public virtual void Visit(NotNode node)
        {
            Nodes.Push(new NotNode(Nodes.Pop()));
        }

        public virtual void Visit(LikeNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LikeNode(left, right));
        }

        public virtual void Visit(FieldNode node)
        {
            Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder, node.FieldName));
        }

        public void Visit(FieldOrderedNode node)
        {
            Nodes.Push(new FieldOrderedNode(Nodes.Pop(), node.FieldOrder, node.FieldName, node.Order));
        }

        public virtual void Visit(SelectNode node)
        {
            var fields = new FieldNode[node.Fields.Length];

            for (var i = node.Fields.Length - 1; i >= 0; --i)
                fields[i] = (FieldNode) Nodes.Pop();

            Nodes.Push(new SelectNode(fields.ToArray()));
        }

        public virtual void Visit(GroupSelectNode node)
        {
            var fields = new FieldNode[node.Fields.Length];

            for (var i = node.Fields.Length - 1; i >= 0; --i)
                fields[i] = (FieldNode) Nodes.Pop();

            Nodes.Push(new GroupSelectNode(fields));
        }

        public virtual void Visit(StringNode node)
        {
            Nodes.Push(new StringNode(node.Value));
        }

        public virtual void Visit(DecimalNode node)
        {
            Nodes.Push(new DecimalNode(node.Value.ToString(CultureInfo.InvariantCulture)));
        }

        public virtual void Visit(IntegerNode node)
        {
            Nodes.Push(new IntegerNode(node.Value.ToString()));
        }

        public void Visit(BooleanNode node)
        {
            Nodes.Push(new BooleanNode(node.Value));
        }

        public virtual void Visit(WordNode node)
        {
            Nodes.Push(new WordNode(node.Value));
        }

        public virtual void Visit(ContainsNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new ContainsNode(left, right as ArgsListNode));
        }

        public virtual void Visit(AccessMethodNode node)
        {
            Nodes.Push(new AccessMethodNode(node.FToken, (ArgsListNode) Nodes.Pop(), null, node.Method, node.Alias));
        }

        public virtual void Visit(IsNullNode node)
        {
            Nodes.Push(new IsNullNode(Nodes.Pop(), node.IsNegated));
        }

        public virtual void Visit(AccessRefreshAggreationScoreNode node)
        {
        }

        public virtual void Visit(AccessColumnNode node)
        {
            Nodes.Push(new AccessColumnNode(node.Name, node.Alias, node.ReturnType, node.Span));
        }

        public virtual void Visit(AllColumnsNode node)
        {
            Nodes.Push(new AllColumnsNode());
        }

        public virtual void Visit(IdentifierNode node)
        {
            Nodes.Push(new IdentifierNode(node.Name));
        }

        public virtual void Visit(AccessObjectArrayNode node)
        {
            var parentNodeType = Nodes.Peek().ReturnType;
            Nodes.Push(new AccessObjectArrayNode(node.Token, parentNodeType.GetProperty(node.Name)));
        }

        public virtual void Visit(AccessObjectKeyNode node)
        {
            var parentNodeType = Nodes.Peek().ReturnType;
            Nodes.Push(new AccessObjectKeyNode(node.Token, parentNodeType.GetProperty(node.ObjectName)));
        }

        public virtual void Visit(PropertyValueNode node)
        {
            var parentNodeType = Nodes.Peek().ReturnType;
            Nodes.Push(new PropertyValueNode(node.Name, parentNodeType.GetProperty(node.Name)));
        }

        public virtual void Visit(DotNode node)
        {
            var exp = Nodes.Pop();
            var root = Nodes.Pop();

            Nodes.Push(new DotNode(root, exp, node.IsOuter, string.Empty, exp.ReturnType));
        }

        public virtual void Visit(AccessCallChainNode node)
        {
            Nodes.Push(new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props, node.Alias));
        }

        public virtual void Visit(ArgsListNode node)
        {
            var args = new Node[node.Args.Length];

            for (var i = node.Args.Length - 1; i >= 0; --i)
                args[i] = Nodes.Pop();

            Nodes.Push(new ArgsListNode(args));
        }

        public virtual void Visit(WhereNode node)
        {
            Nodes.Push(new WhereNode(Nodes.Pop()));
        }

        public virtual void Visit(GroupByNode node)
        {
            var having = Nodes.Peek() as HavingNode;

            if (having != null)
                Nodes.Pop();

            var fields = new FieldNode[node.Fields.Length];

            for (var i = node.Fields.Length - 1; i >= 0; --i) fields[i] = Nodes.Pop() as FieldNode;

            Nodes.Push(new GroupByNode(fields, having));
        }

        public virtual void Visit(HavingNode node)
        {
            Nodes.Push(new HavingNode(Nodes.Pop()));
        }

        public virtual void Visit(SkipNode node)
        {
            Nodes.Push(new SkipNode((IntegerNode) node.Expression));
        }

        public virtual void Visit(TakeNode node)
        {
            Nodes.Push(new TakeNode((IntegerNode) node.Expression));
        }

        public virtual void Visit(SchemaFromNode node)
        {
            Nodes.Push(new SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias));
        }

        public virtual void Visit(JoinSourcesTableFromNode node)
        {
            var exp = Nodes.Pop();
            var b = (FromNode) Nodes.Pop();
            var a = (FromNode) Nodes.Pop();

            Nodes.Push(new JoinSourcesTableFromNode(a, b, exp));
        }

        public virtual void Visit(InMemoryTableFromNode node)
        {
            Nodes.Push(new InMemoryTableFromNode(node.VariableName, node.Alias));
        }

        public virtual void Visit(JoinFromNode node)
        {
            var expression = Nodes.Pop();
            var joinedTable = (FromNode) Nodes.Pop();
            var source = (FromNode) Nodes.Pop();
            var joinedFrom = new JoinFromNode(source, joinedTable, expression, node.JoinType);
            Nodes.Push(joinedFrom);
        }

        public virtual void Visit(ExpressionFromNode node)
        {
            var from = (FromNode) Nodes.Pop();
            Nodes.Push(new ExpressionFromNode(from));
        }

        public virtual void Visit(CreateTableNode node)
        {
            var items = new FieldNode[node.Fields.Length];

            for (var i = node.Fields.Length - 1; i >= 0; --i)
                items[i] = (FieldNode) Nodes.Pop();

            Nodes.Push(new CreateTableNode(node.Name, node.Keys, items));
        }

        public virtual void Visit(RenameTableNode node)
        {
            Nodes.Push(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
        }

        public virtual void Visit(TranslatedSetTreeNode node)
        {
        }

        public virtual void Visit(IntoNode node)
        {
            Nodes.Push(new IntoNode(node.Name));
        }

        public virtual void Visit(QueryScope node)
        {
        }

        public virtual void Visit(ShouldBePresentInTheTable node)
        {
            Nodes.Push(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
        }

        public virtual void Visit(TranslatedSetOperatorNode node)
        {
        }

        public virtual void Visit(QueryNode node)
        {
            var orderBy = node.OrderBy != null ? Nodes.Pop() as OrderByNode : null;
            var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;

            var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
            var take = node.Take != null ? Nodes.Pop() as TakeNode : null;

            var select = Nodes.Pop() as SelectNode;
            var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
            var from = Nodes.Pop() as FromNode;

            Nodes.Push(new QueryNode(select, from, where, groupBy, null, skip, take));
        }

        public virtual void Visit(JoinInMemoryWithSourceTableFromNode node)
        {
            var exp = Nodes.Pop();
            var from = (FromNode) Nodes.Pop();
            Nodes.Push(new JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp));
        }

        public virtual void Visit(InternalQueryNode node)
        {
            throw new NotSupportedException();
        }

        public virtual void Visit(RootNode node)
        {
            Nodes.Push(new RootNode(Nodes.Pop()));
        }

        public virtual void Visit(SingleSetNode node)
        {
        }

        public virtual void Visit(RefreshNode node)
        {
            Nodes.Push(new RefreshNode(node.Nodes));
        }

        public virtual void Visit(UnionNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();

            Nodes.Push(new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public virtual void Visit(UnionAllNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();

            Nodes.Push(new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
                node.IsTheLastOne));
        }

        public virtual void Visit(ExceptNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public virtual void Visit(IntersectNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(
                new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public virtual void Visit(PutTrueNode node)
        {
            Nodes.Push(new PutTrueNode());
        }

        public virtual void Visit(MultiStatementNode node)
        {
            var items = new Node[node.Nodes.Length];

            for (var i = node.Nodes.Length - 1; i >= 0; --i)
                items[i] = Nodes.Pop();

            Nodes.Push(new MultiStatementNode(items, node.ReturnType));
        }

        public virtual void Visit(CteExpressionNode node)
        {
            var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

            for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
                sets[i] = (CteInnerExpressionNode) Nodes.Pop();

            Nodes.Push(new CteExpressionNode(sets, Nodes.Pop()));
        }

        public virtual void Visit(CteInnerExpressionNode node)
        {
            Nodes.Push(new CteInnerExpressionNode(Nodes.Pop(), node.Name));
        }

        public virtual void Visit(JoinsNode node)
        {
            Nodes.Push(new JoinsNode((JoinFromNode) Nodes.Pop()));
        }

        public virtual void Visit(JoinNode node)
        {
            var expression = Nodes.Pop();
            var fromNode = (FromNode) Nodes.Pop();

            if (node is OuterJoinNode outerJoin)
                Nodes.Push(new OuterJoinNode(outerJoin.Type, fromNode, expression));
            else
                Nodes.Push(new InnerJoinNode(fromNode, expression));
        }

        public void Visit(OrderByNode node)
        {
            var fields = new FieldOrderedNode[node.Fields.Length];

            for (var i = node.Fields.Length - 1; i >= 0; --i)
                fields[i] = (FieldOrderedNode)Nodes.Pop();

            Nodes.Push(new OrderByNode(fields));
        }
    }
}