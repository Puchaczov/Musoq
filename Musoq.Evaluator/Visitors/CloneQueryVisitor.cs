using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public class CloneQueryVisitor : IExpressionVisitor
{
    protected Stack<Node> Nodes { get; } = new();

    public RootNode Root => (RootNode) Nodes.Peek();

    public void Visit(Node node)
    {
    }

    public virtual void Visit(DescNode node)
    {
        Nodes.Push(new DescNode((FromNode) Nodes.Pop(), node.Type));
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

    public virtual void Visit(RLikeNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new RLikeNode(left, right));
    }

    public virtual void Visit(InNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new InNode(left, (ArgsListNode)right));
    }

    public virtual void Visit(FieldNode node)
    {
        Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder, node.FieldName));
    }

    public virtual void Visit(FieldOrderedNode node)
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
        Nodes.Push(new DecimalNode(node.Value));
    }

    public virtual void Visit(IntegerNode node)
    {
        Nodes.Push(new IntegerNode(node.ObjValue));
    }

    public void Visit(BooleanNode node)
    {
        Nodes.Push(new BooleanNode(node.Value));
    }

    public virtual void Visit(WordNode node)
    {
        Nodes.Push(new WordNode(node.Value));
    }

    public virtual void Visit(NullNode node)
    {
        Nodes.Push(new NullNode());
    }

    public virtual void Visit(ContainsNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new ContainsNode(left, right as ArgsListNode));
    }

    public virtual void Visit(AccessMethodNode node)
    {
        Nodes.Push(new AccessMethodNode(node.FunctionToken, (ArgsListNode) Nodes.Pop(), null, node.CanSkipInjectSource, node.Method, node.Alias));
    }

    public virtual void Visit(AccessRawIdentifierNode node)
    {
        Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
    }

    public virtual void Visit(IsNullNode node)
    {
        Nodes.Push(new IsNullNode(Nodes.Pop(), node.IsNegated));
    }

    public virtual void Visit(AccessRefreshAggregationScoreNode node)
    {
    }

    public virtual void Visit(AccessColumnNode node)
    {
        Nodes.Push(new AccessColumnNode(node.Name, node.Alias, node.ReturnType, node.Span));
    }

    public virtual void Visit(AllColumnsNode node)
    {
        Nodes.Push(new AllColumnsNode(node.Alias));
    }

    public virtual void Visit(IdentifierNode node)
    {
        Nodes.Push(new IdentifierNode(node.Name));
    }

    public virtual void Visit(AccessObjectArrayNode node)
    {
        Nodes.Push(new AccessObjectArrayNode(node.Token, node.PropertyInfo));
    }

    public virtual void Visit(AccessObjectKeyNode node)
    {
        Nodes.Push(new AccessObjectKeyNode(node.Token, node.PropertyInfo));
    }

    public virtual void Visit(PropertyValueNode node)
    {
        Nodes.Push(new PropertyValueNode(node.Name, node.PropertyInfo));
    }

    public virtual void Visit(DotNode node)
    {
        var exp = Nodes.Pop();
        var root = Nodes.Pop();

        Nodes.Push(new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType));
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
        Nodes.Push(
            node is Parser.SchemaFromNode schemaFromNode ?
                new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias, node.QueryId, schemaFromNode.HasExternallyProvidedTypes) :
                new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias, node.QueryId, false));
    }

    public virtual void Visit(JoinSourcesTableFromNode node)
    {
        var exp = Nodes.Pop();
        var b = (FromNode) Nodes.Pop();
        var a = (FromNode) Nodes.Pop();

        Nodes.Push(new Parser.JoinSourcesTableFromNode(a, b, exp, node.JoinType));
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
        var b = (FromNode) Nodes.Pop();
        var a = (FromNode) Nodes.Pop();

        Nodes.Push(new Parser.ApplySourcesTableFromNode(a, b, node.ApplyType));
    }

    public virtual void Visit(InMemoryTableFromNode node)
    {
        Nodes.Push(new Parser.InMemoryTableFromNode(node.VariableName, node.Alias));
    }

    public virtual void Visit(JoinFromNode node)
    {
        var expression = Nodes.Pop();
        var joinedTable = (FromNode) Nodes.Pop();
        var source = (FromNode) Nodes.Pop();
        var joinedFrom = new Parser.JoinFromNode(source, joinedTable, expression, node.JoinType);
        Nodes.Push(joinedFrom);
    }

    public void Visit(ApplyFromNode node)
    {
        var appliedTable = (FromNode) Nodes.Pop();
        var source = (FromNode) Nodes.Pop();
        var appliedFrom = new Parser.ApplyFromNode(source, appliedTable, node.ApplyType);
        Nodes.Push(appliedFrom);
    }

    public virtual void Visit(ExpressionFromNode node)
    {
        var from = (FromNode) Nodes.Pop();
        Nodes.Push(new Parser.ExpressionFromNode(from));
    }

    public virtual void Visit(AccessMethodFromNode node)
    {
        Nodes.Push(new Parser.AccessMethodFromNode(node.Alias, node.SourceAlias, (AccessMethodNode)Nodes.Pop(), node.ReturnType));
    }

    public virtual void Visit(PropertyFromNode node)
    {
        Nodes.Push(new Parser.PropertyFromNode(node.Alias, node.SourceAlias, node.PropertiesChain));
    }

    public virtual void Visit(AliasedFromNode node)
    {
        Nodes.Push(new Parser.AliasedFromNode(node.Identifier, (ArgsListNode)Nodes.Pop(), node.Alias, node.InSourcePosition));
    }

    public virtual void Visit(SchemaMethodFromNode node)
    {
        Nodes.Push(new Parser.SchemaMethodFromNode(node.Alias, node.Schema, node.Method));
    }

    public virtual void Visit(CreateTransformationTableNode node)
    {
        var items = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            items[i] = (FieldNode) Nodes.Pop();

        Nodes.Push(new CreateTransformationTableNode(node.Name, node.Keys, items, node.ForGrouping));
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

        Nodes.Push(new QueryNode(select, from, where, groupBy, orderBy, skip, take));
    }

    public virtual void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        var exp = Nodes.Pop();
        var from = (FromNode) Nodes.Pop();
        Nodes.Push(new Parser.JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp, node.JoinType));
    }

    public virtual void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        var from = (FromNode) Nodes.Pop();
        Nodes.Push(new Parser.ApplyInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, node.ApplyType));
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

    public virtual void Visit(JoinNode node)
    {
        Nodes.Push(new Parser.JoinNode((Parser.JoinFromNode) Nodes.Pop()));
    }

    public void Visit(ApplyNode node)
    {
        Nodes.Push(new Parser.ApplyNode((Parser.ApplyFromNode) Nodes.Pop()));
    }

    public virtual void Visit(OrderByNode node)
    {
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldOrderedNode)Nodes.Pop();

        Nodes.Push(new OrderByNode(fields));
    }

    public virtual void Visit(CreateTableNode node)
    {
        Nodes.Push(new CreateTableNode(node.Name, node.TableTypePairs));
    }

    public virtual void Visit(CoupleNode node)
    {
        Nodes.Push(new CoupleNode(node.SchemaMethodNode, node.TableName, node.MappedSchemaName));
    }

    public virtual void Visit(StatementsArrayNode node)
    {
        var statements = new StatementNode[node.Statements.Length];
        for(var i = 0; i < node.Statements.Length; ++i)
        {
            statements[node.Statements.Length - 1 - i] = (StatementNode)Nodes.Pop();
        }

        Nodes.Push(new StatementsArrayNode(statements));
    }

    public virtual void Visit(StatementNode node)
    {
        Nodes.Push(new StatementNode(Nodes.Pop()));
    }

    public virtual void Visit(CaseNode node)
    {
        var whenThenPairs = new List<(Node When, Node Then)>();

        for (int i = 0; i < node.WhenThenPairs.Length; ++i)
        {
            var then = Nodes.Pop();
            var when = Nodes.Pop();
            whenThenPairs.Add((when, then));
        }

        var elseNode = Nodes.Pop();

        Nodes.Push(new CaseNode(whenThenPairs.ToArray(), elseNode, node.ReturnType));
    }

    public virtual void Visit(WhenNode node)
    {
        Nodes.Push(new WhenNode(Nodes.Pop()));
    }

    public virtual void Visit(ThenNode node)
    {
        Nodes.Push(new ThenNode(Nodes.Pop()));
    }

    public virtual void Visit(ElseNode node)
    {
        Nodes.Push(new ElseNode(Nodes.Pop()));
    }

    public virtual void Visit(FieldLinkNode node)
    {
        Nodes.Push(new FieldLinkNode($"::{node.Index}", node.ReturnType));
    }
}