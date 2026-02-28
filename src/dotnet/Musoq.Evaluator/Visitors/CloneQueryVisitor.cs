using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public class CloneQueryVisitor : DefensiveVisitorBase, IExpressionVisitor
{
    protected Stack<Node> Nodes { get; } = new();

    /// <summary>
    ///     Gets the name of this visitor for error reporting.
    /// </summary>
    protected override string VisitorName => nameof(CloneQueryVisitor);

    public RootNode Root => SafeCast<RootNode>(SafePeek(Nodes, VisitorOperationNames.GettingRoot),
        VisitorOperationNames.GettingRoot);

    public override void Visit(Node node)
    {
    }

    public override void Visit(DescNode node)
    {
        var fromNode = SafeCast<FromNode>(SafePop(Nodes, VisitorOperationNames.VisitDescNode),
            VisitorOperationNames.VisitDescNode);
        Nodes.Push(new DescNode(fromNode, node.Type, node.Column));
    }

    public override void Visit(StarNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitStarNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new StarNode(left, right));
    }

    public override void Visit(FSlashNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitFSlashNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new FSlashNode(left, right));
    }

    public override void Visit(ModuloNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitModuloNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new ModuloNode(left, right));
    }

    public override void Visit(AddNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitAddNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new AddNode(left, right));
    }

    public override void Visit(HyphenNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitHyphenNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new HyphenNode(left, right));
    }

    public override void Visit(AndNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitAndNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new AndNode(left, right));
    }

    public override void Visit(OrNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitOrNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new OrNode(left, right));
    }

    public override void Visit(BitwiseAndNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitBitwiseAndNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new BitwiseAndNode(left, right));
    }

    public override void Visit(BitwiseOrNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitBitwiseOrNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new BitwiseOrNode(left, right));
    }

    public override void Visit(BitwiseXorNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitBitwiseXorNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new BitwiseXorNode(left, right));
    }

    public override void Visit(LeftShiftNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitLeftShiftNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new LeftShiftNode(left, right));
    }

    public override void Visit(RightShiftNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitRightShiftNode);
        var right = nodes[1];
        var left = nodes[0];
        Nodes.Push(new RightShiftNode(left, right));
    }

    public override void Visit(ShortCircuitingNodeLeft node)
    {
        Nodes.Push(new ShortCircuitingNodeLeft(Nodes.Pop(), node.UsedFor));
    }

    public override void Visit(ShortCircuitingNodeRight node)
    {
        Nodes.Push(new ShortCircuitingNodeRight(Nodes.Pop(), node.UsedFor));
    }

    public override void Visit(EqualityNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new EqualityNode(left, right));
    }

    public override void Visit(GreaterOrEqualNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new GreaterOrEqualNode(left, right));
    }

    public override void Visit(LessOrEqualNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new LessOrEqualNode(left, right));
    }

    public override void Visit(GreaterNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new GreaterNode(left, right));
    }

    public override void Visit(LessNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new LessNode(left, right));
    }

    public override void Visit(DiffNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new DiffNode(left, right));
    }

    public override void Visit(NotNode node)
    {
        Nodes.Push(new NotNode(Nodes.Pop()));
    }

    public override void Visit(LikeNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new LikeNode(left, right));
    }

    public override void Visit(RLikeNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new RLikeNode(left, right));
    }

    public override void Visit(InNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new InNode(left, (ArgsListNode)right));
    }

    public override void Visit(BetweenNode node)
    {
        var max = Nodes.Pop();
        var min = Nodes.Pop();
        var expression = Nodes.Pop();
        Nodes.Push(new BetweenNode(expression, min, max));
    }

    public override void Visit(FieldNode node)
    {
        Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder, node.FieldName));
    }

    public override void Visit(FieldOrderedNode node)
    {
        Nodes.Push(new FieldOrderedNode(Nodes.Pop(), node.FieldOrder, node.FieldName, node.Order));
    }

    public override void Visit(SelectNode node)
    {
        var fields = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldNode)Nodes.Pop();

        Nodes.Push(new SelectNode(fields.ToArray(), node.IsDistinct));
    }

    public override void Visit(GroupSelectNode node)
    {
        var fields = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldNode)Nodes.Pop();

        Nodes.Push(new GroupSelectNode(fields));
    }

    public override void Visit(StringNode node)
    {
        Nodes.Push(new StringNode(node.Value));
    }

    public override void Visit(DecimalNode node)
    {
        Nodes.Push(new DecimalNode(node.Value));
    }

    public override void Visit(IntegerNode node)
    {
        Nodes.Push(new IntegerNode(node.ObjValue));
    }

    public override void Visit(HexIntegerNode node)
    {
        Nodes.Push(new HexIntegerNode(node.ObjValue));
    }

    public override void Visit(BinaryIntegerNode node)
    {
        Nodes.Push(new BinaryIntegerNode(node.ObjValue));
    }

    public override void Visit(OctalIntegerNode node)
    {
        Nodes.Push(new OctalIntegerNode(node.ObjValue));
    }

    public override void Visit(BooleanNode node)
    {
        Nodes.Push(new BooleanNode(node.Value));
    }

    public override void Visit(WordNode node)
    {
        Nodes.Push(new WordNode(node.Value));
    }

    public override void Visit(NullNode node)
    {
        Nodes.Push(new NullNode());
    }

    public override void Visit(ContainsNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new ContainsNode(left, right as ArgsListNode));
    }

    public override void Visit(AccessMethodNode node)
    {
        Nodes.Push(new AccessMethodNode(node.FunctionToken, (ArgsListNode)Nodes.Pop(), null, node.CanSkipInjectSource,
            node.Method, node.Alias, default, node.IsDistinct));
    }

    public override void Visit(AccessRawIdentifierNode node)
    {
        Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
    }

    public override void Visit(IsNullNode node)
    {
        Nodes.Push(new IsNullNode(Nodes.Pop(), node.IsNegated));
    }

    public override void Visit(AccessRefreshAggregationScoreNode node)
    {
    }

    public override void Visit(AccessColumnNode node)
    {
        Nodes.Push(new AccessColumnNode(node.Name, node.Alias, node.ReturnType, node.Span, node.IntendedTypeName));
    }

    public override void Visit(AllColumnsNode node)
    {
        Nodes.Push(new AllColumnsNode(node.Alias));
    }

    public override void Visit(IdentifierNode node)
    {
        Nodes.Push(new IdentifierNode(node.Name));
    }

    public override void Visit(AccessObjectArrayNode node)
    {
        if (node.IsColumnAccess)
            Nodes.Push(new AccessObjectArrayNode(node.Token, node.ColumnType, node.TableAlias, node.IntendedTypeName));
        else
            Nodes.Push(new AccessObjectArrayNode(node.Token, node.PropertyInfo));
    }

    public override void Visit(AccessObjectKeyNode node)
    {
        var clonedNode = new AccessObjectKeyNode(node.Token, node.PropertyInfo);
        clonedNode.DestinationKind = node.DestinationKind;
        Nodes.Push(clonedNode);
    }

    public override void Visit(PropertyValueNode node)
    {
        Nodes.Push(new PropertyValueNode(node.Name, node.PropertyInfo));
    }

    public override void Visit(DotNode node)
    {
        var exp = Nodes.Pop();
        var root = Nodes.Pop();

        Nodes.Push(new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType, node.IntendedTypeName));
    }

    public override void Visit(AccessCallChainNode node)
    {
        Nodes.Push(new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props, node.Alias));
    }

    public override void Visit(ArgsListNode node)
    {
        var args = new Node[node.Args.Length];

        for (var i = node.Args.Length - 1; i >= 0; --i)
            args[i] = Nodes.Pop();

        Nodes.Push(new ArgsListNode(args));
    }

    public override void Visit(WhereNode node)
    {
        Nodes.Push(new WhereNode(Nodes.Pop()));
    }

    public override void Visit(GroupByNode node)
    {
        var having = Nodes.Peek() as HavingNode;

        if (having != null)
            Nodes.Pop();

        var fields = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i) fields[i] = Nodes.Pop() as FieldNode;

        Nodes.Push(new GroupByNode(fields, having));
    }

    public override void Visit(HavingNode node)
    {
        Nodes.Push(new HavingNode(Nodes.Pop()));
    }

    public override void Visit(SkipNode node)
    {
        Nodes.Push(new SkipNode((IntegerNode)node.Expression));
    }

    public override void Visit(TakeNode node)
    {
        Nodes.Push(new TakeNode((IntegerNode)node.Expression));
    }

    public override void Visit(SchemaFromNode node)
    {
        Nodes.Push(
            node is Parser.SchemaFromNode schemaFromNode
                ? new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias,
                    node.QueryId, schemaFromNode.HasExternallyProvidedTypes)
                : new Parser.SchemaFromNode(node.Schema, node.Method, (ArgsListNode)Nodes.Pop(), node.Alias,
                    node.QueryId, false));
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        var exp = Nodes.Pop();
        var b = (FromNode)Nodes.Pop();
        var a = (FromNode)Nodes.Pop();

        Nodes.Push(new Parser.JoinSourcesTableFromNode(a, b, exp, node.JoinType));
    }

    public override void Visit(ApplySourcesTableFromNode node)
    {
        var b = (FromNode)Nodes.Pop();
        var a = (FromNode)Nodes.Pop();

        Nodes.Push(new Parser.ApplySourcesTableFromNode(a, b, node.ApplyType));
    }

    public override void Visit(InMemoryTableFromNode node)
    {
        Nodes.Push(new Parser.InMemoryTableFromNode(node.VariableName, node.Alias));
    }

    public override void Visit(JoinFromNode node)
    {
        var expression = Nodes.Pop();
        var joinedTable = (FromNode)Nodes.Pop();
        var source = (FromNode)Nodes.Pop();
        var joinedFrom = new Parser.JoinFromNode(source, joinedTable, expression, node.JoinType);
        Nodes.Push(joinedFrom);
    }

    public override void Visit(ApplyFromNode node)
    {
        var appliedTable = (FromNode)Nodes.Pop();
        var source = (FromNode)Nodes.Pop();
        var appliedFrom = new Parser.ApplyFromNode(source, appliedTable, node.ApplyType);
        Nodes.Push(appliedFrom);
    }

    public override void Visit(ExpressionFromNode node)
    {
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.ExpressionFromNode(from));
    }

    public override void Visit(InterpretFromNode node)
    {
        var interpretCall = Nodes.Pop();
        Nodes.Push(new Parser.InterpretFromNode(node.Alias, interpretCall, node.ApplyType, node.ReturnType));
    }

    public override void Visit(AccessMethodFromNode node)
    {
        Nodes.Push(new Parser.AccessMethodFromNode(node.Alias, node.SourceAlias, (AccessMethodNode)Nodes.Pop(),
            node.ReturnType));
    }

    public override void Visit(PropertyFromNode node)
    {
        Nodes.Push(new Parser.PropertyFromNode(node.Alias, node.SourceAlias, node.PropertiesChain));
    }

    public override void Visit(AliasedFromNode node)
    {
        Nodes.Push(new Parser.AliasedFromNode(node.Identifier, (ArgsListNode)Nodes.Pop(), node.Alias,
            node.InSourcePosition));
    }

    public override void Visit(SchemaMethodFromNode node)
    {
        Nodes.Push(new Parser.SchemaMethodFromNode(node.Alias, node.Schema, node.Method));
    }

    public override void Visit(CreateTransformationTableNode node)
    {
        var items = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            items[i] = (FieldNode)Nodes.Pop();

        Nodes.Push(new CreateTransformationTableNode(node.Name, node.Keys, items, node.ForGrouping));
    }

    public override void Visit(RenameTableNode node)
    {
        Nodes.Push(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
    }

    public override void Visit(TranslatedSetTreeNode node)
    {
    }

    public override void Visit(IntoNode node)
    {
        Nodes.Push(new IntoNode(node.Name));
    }

    public override void Visit(QueryScope node)
    {
    }

    public override void Visit(ShouldBePresentInTheTable node)
    {
        Nodes.Push(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
    }

    public override void Visit(TranslatedSetOperatorNode node)
    {
    }

    public override void Visit(QueryNode node)
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

    public override void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        var exp = Nodes.Pop();
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp, node.JoinType));
    }

    public override void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        var from = (FromNode)Nodes.Pop();
        Nodes.Push(new Parser.ApplyInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, node.ApplyType));
    }

    public override void Visit(InternalQueryNode node)
    {
        throw new NotSupportedException();
    }

    public override void Visit(RootNode node)
    {
        Nodes.Push(new RootNode(Nodes.Pop()));
    }

    public override void Visit(SingleSetNode node)
    {
    }

    public override void Visit(RefreshNode node)
    {
        Nodes.Push(new RefreshNode(node.Nodes));
    }

    public override void Visit(UnionNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        Nodes.Push(new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
    }

    public override void Visit(UnionAllNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        Nodes.Push(new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
            node.IsTheLastOne));
    }

    public override void Visit(ExceptNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
    }

    public override void Visit(IntersectNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(
            new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
    }

    public override void Visit(PutTrueNode node)
    {
        Nodes.Push(new PutTrueNode());
    }

    public override void Visit(MultiStatementNode node)
    {
        var items = new Node[node.Nodes.Length];

        for (var i = node.Nodes.Length - 1; i >= 0; --i)
            items[i] = Nodes.Pop();

        Nodes.Push(new MultiStatementNode(items, node.ReturnType));
    }

    public override void Visit(BinarySchemaNode node)
    {
        Nodes.Push(node);
    }

    public override void Visit(TextSchemaNode node)
    {
        Nodes.Push(node);
    }

    public override void Visit(CteExpressionNode node)
    {
        var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

        for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
            sets[i] = (CteInnerExpressionNode)Nodes.Pop();

        Nodes.Push(new CteExpressionNode(sets, Nodes.Pop()));
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        Nodes.Push(new CteInnerExpressionNode(Nodes.Pop(), node.Name));
    }

    public override void Visit(JoinNode node)
    {
        Nodes.Push(new Parser.JoinNode((Parser.JoinFromNode)Nodes.Pop()));
    }

    public override void Visit(ApplyNode node)
    {
        Nodes.Push(new Parser.ApplyNode((Parser.ApplyFromNode)Nodes.Pop()));
    }

    public override void Visit(OrderByNode node)
    {
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldOrderedNode)Nodes.Pop();

        Nodes.Push(new OrderByNode(fields));
    }

    public override void Visit(CreateTableNode node)
    {
        Nodes.Push(new CreateTableNode(node.Name, node.TableTypePairs));
    }

    public override void Visit(CoupleNode node)
    {
        Nodes.Push(new CoupleNode(node.SchemaMethodNode, node.TableName, node.MappedSchemaName));
    }

    public override void Visit(StatementsArrayNode node)
    {
        var statements = new StatementNode[node.Statements.Length];
        for (var i = 0; i < node.Statements.Length; ++i)
            statements[node.Statements.Length - 1 - i] = (StatementNode)Nodes.Pop();

        Nodes.Push(new StatementsArrayNode(statements));
    }

    public override void Visit(StatementNode node)
    {
        Nodes.Push(new StatementNode(Nodes.Pop()));
    }

    public override void Visit(CaseNode node)
    {
        var whenThenPairs = new List<(Node When, Node Then)>();

        for (var i = 0; i < node.WhenThenPairs.Length; ++i)
        {
            var then = Nodes.Pop();
            var when = Nodes.Pop();
            whenThenPairs.Add((when, then));
        }

        var elseNode = Nodes.Pop();

        Nodes.Push(new CaseNode(whenThenPairs.ToArray(), elseNode, node.ReturnType));
    }

    public override void Visit(WhenNode node)
    {
        Nodes.Push(new WhenNode(Nodes.Pop()));
    }

    public override void Visit(ThenNode node)
    {
        Nodes.Push(new ThenNode(Nodes.Pop()));
    }

    public override void Visit(ElseNode node)
    {
        Nodes.Push(new ElseNode(Nodes.Pop()));
    }

    public override void Visit(FieldLinkNode node)
    {
        Nodes.Push(new FieldLinkNode($"::{node.Index}", node.ReturnType));
    }

    public override void Visit(InterpretCallNode node)
    {
        var dataSource = Nodes.Pop();
        Nodes.Push(new InterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(ParseCallNode node)
    {
        var dataSource = Nodes.Pop();
        Nodes.Push(new ParseCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(TryInterpretCallNode node)
    {
        var dataSource = Nodes.Pop();
        Nodes.Push(new TryInterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(TryParseCallNode node)
    {
        var dataSource = Nodes.Pop();
        Nodes.Push(new TryParseCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(PartialInterpretCallNode node)
    {
        var dataSource = Nodes.Pop();
        Nodes.Push(new PartialInterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(InterpretAtCallNode node)
    {
        var offset = Nodes.Pop();
        var dataSource = Nodes.Pop();
        Nodes.Push(new InterpretAtCallNode(dataSource, offset, node.SchemaName, node.ReturnType));
    }
}
