using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal static class ParserNodeTraversalRegistry
{
    private static readonly ParserNodeTraversalDescriptor[] RegisteredDescriptors =
    [
        Children<SelectNode>(static node => node.Fields, includeDerivedTypes: true),
        Children<ParameterBlockNode>(static node => node.Parameters),
        Children<ParameterDeclarationNode>(static node => Optional(node.DefaultValue)),
        Children<BinaryNode>(static node => Optional(node.Left, node.Right), includeDerivedTypes: true),
        Children<AccessRefreshAggregationScoreNode>(static node => Optional(node.FilterExpression, node.Arguments)),
        Children<AccessMethodNode>(static node => Optional(node.FilterExpression, node.Arguments)),
        Children<IsNullNode>(static node => Optional(node.Expression)),
        Children<RowPresenceNode>(static node => Optional(node.Expression)),
        Children<AllColumnsNode>(AllColumnsChildren),
        Children<DotNode>(static node => Optional(node.Root, node.Expression)),
        Children<RootNode>(static node => Optional(node.Expression)),
        Children<SingleSetNode>(static node => Optional(node.Query)),
        Children<RefreshNode>(static node => node.Nodes),
        Children<MultiStatementNode>(static node => node.Nodes),
        Leaf<DiagnosticCommandNode>(),
        Special<CteExpressionNode>(CteChildren),
        Children<CteInnerExpressionNode>(static node => Optional(node.Value)),
        Children<JoinNode>(static node => Optional(node.Join), includeDerivedTypes: true),
        Children<ApplyNode>(static node => Optional(node.Apply), includeDerivedTypes: true),
        Children<OrderByNode>(static node => node.Fields),
        Children<StatementsArrayNode>(static node => node.Statements),
        Children<StatementNode>(static node => Optional(node.Node)),
        Children<CaseNode>(CaseChildren),
        Children<WhenNode>(static node => Optional(node.Expression)),
        Children<ThenNode>(static node => Optional(node.Expression)),
        Children<ElseNode>(static node => Optional(node.Expression)),
        Children<WindowFunctionNode>(static node => Optional(node.FunctionCall, node.WindowSpecification)),
        Children<WindowSpecificationNode>(WindowSpecificationChildren),
        Children<WindowFrameNode>(static node => Optional(node.Start, node.End)),
        Leaf<WindowFrameBoundNode>(),
        Children<WindowDefinitionNode>(static node => Optional(node.Specification)),
        Children<WindowNode>(static node => node.Definitions),
        Children<QualifyNode>(static node => Optional(node.Expression)),
        Children<WhereNode>(static node => Optional(node.Expression)),
        Children<GroupByNode>(GroupByChildren),
        Children<HavingNode>(static node => Optional(node.Expression)),
        Children<JoinInMemoryWithSourceTableFromNode>(static node => Optional(node.SourceTable, node.Expression, node.TieBreak), includeDerivedTypes: true),
        Children<ApplyInMemoryWithSourceTableFromNode>(static node => Optional(node.SourceTable), includeDerivedTypes: true),
        Children<SchemaFromNode>(static node => Optional(node.Parameters), includeDerivedTypes: true),
        Children<JoinSourcesTableFromNode>(static node => Optional(node.Expression, node.First, node.Second, node.TieBreak), includeDerivedTypes: true),
        Children<ApplySourcesTableFromNode>(static node => Optional(node.First, node.Second), includeDerivedTypes: true),
        Children<DerivedTableFromNode>(static node => Optional(node.Query)),
        Leaf<InMemoryTableFromNode>(includeDerivedTypes: true),
        Leaf<InMemoryGroupedFromNode>(includeDerivedTypes: true),
        Children<ValuesFromNode>(ValuesChildren),
        Children<UnpivotFromNode>(UnpivotChildren),
        Children<JoinFromNode>(static node => Optional(node.Source, node.With, node.Expression, node.TieBreak), includeDerivedTypes: true),
        Children<ApplyFromNode>(static node => Optional(node.Source, node.With), includeDerivedTypes: true),
        Children<ExpressionFromNode>(static node => Optional(node.Expression), includeDerivedTypes: true),
        Children<InterpretFromNode>(static node => Optional(node.InterpretCall), includeDerivedTypes: true),
        Children<AccessMethodFromNode>(static node => Optional(node.AccessMethod), includeDerivedTypes: true),
        Leaf<PropertyFromNode>(includeDerivedTypes: true),
        Leaf<ReferentialFromNode>(),
        Leaf<SchemaMethodFromNode>(includeDerivedTypes: true),
        Children<AliasedFromNode>(static node => Optional(node.Args), includeDerivedTypes: true),
        Children<CreateTransformationTableNode>(static node => node.Fields),
        Children<TranslatedSetTreeNode>(static node => node.Nodes),
        Special<TranslatedSetOperatorNode>(TranslatedSetOperatorChildren),
        Special<QueryNode>(QueryChildren, includeDerivedTypes: true),
        Children<InterpretCallNode>(static node => Optional(node.DataSource)),
        Children<ParseCallNode>(static node => Optional(node.DataSource)),
        Children<InterpretAtCallNode>(static node => Optional(node.DataSource, node.Offset)),
        Children<TryInterpretCallNode>(static node => Optional(node.DataSource)),
        Children<TryParseCallNode>(static node => Optional(node.DataSource)),
        Children<PartialInterpretCallNode>(static node => Optional(node.DataSource)),
        Children<PartialParseCallNode>(static node => Optional(node.DataSource)),
        Children<FieldDefinitionNode>(static node => Optional(node.AtOffset, node.WhenCondition, node.Constraint)),
        Children<ComputedFieldNode>(static node => Optional(node.Expression)),
        Children<FieldConstraintNode>(static node => Optional(node.Expression)),
        Children<ArrayTypeNode>(static node => Optional(node.ElementType, node.SizeExpression)),
        Children<InlineSchemaTypeNode>(static node => node.Fields),
        Children<ShortCircuitingNodeLeft>(static node => Optional(node.Expression)),
        Children<ShortCircuitingNodeRight>(static node => Optional(node.Expression)),
        Children<NotNode>(static node => Optional(node.Expression)),
        Children<InQueryNode>(static node => Optional(node.Left, node.Subquery)),
        Children<ExistsQueryNode>(static node => Optional(node.Subquery)),
        Children<ScalarSubqueryNode>(static node => Optional(node.Subquery)),
        Children<BetweenNode>(static node => Optional(node.Expression, node.Min, node.Max)),
        Children<CastNode>(static node => Optional(node.Expression)),
        Children<FieldNode>(static node => Optional(node.Expression), includeDerivedTypes: true),
        Children<ArgsListNode>(static node => node.Args),
        Children<DescNode>(static node => Optional(node.Query ?? node.From)),
        Children<ArrayIndexNode>(static node => Optional(node.Array, node.Index)),
        Children<ScriptVariableDeclarationNode>(static node => Optional(node.Initializer)),
        Leaf<AlignmentNode>(),
        Leaf<BinarySchemaNode>(),
        Leaf<BinarySwitchTypeNode>(),
        Leaf<BitsTypeNode>(),
        Leaf<ByteArrayTypeNode>(),
        Leaf<FieldValueValidationNode>(),
        Leaf<PrimitiveTypeNode>(),
        Leaf<RepeatUntilTypeNode>(),
        Leaf<SchemaReferenceTypeNode>(),
        Leaf<StringTypeNode>(),
        Leaf<SubstreamTypeNode>(),
        Leaf<TextFieldDefinitionNode>(),
        Leaf<TextSchemaNode>(),
        Leaf<PutTrueNode>(),
        Leaf<CreateTableNode>(),
        Leaf<CoupleNode>(),
        Leaf<FromNode>(),
        Leaf<IntoNode>(),
        Leaf<QueryScope>(),
        Leaf<RenameTableNode>(),
        Leaf<ScriptVariableReferenceNode>(),
        Leaf<ShouldBePresentInTheTable>(),
        Leaf<SkipNode>(),
        Leaf<StarReplaceItemNode>(),
        Leaf<TakeNode>(),
        Leaf<AccessRawIdentifierNode>(),
        Leaf<AccessColumnNode>(),
        Leaf<AccessObjectArrayNode>(),
        Leaf<AccessObjectKeyNode>(),
        Leaf<PropertyValueNode>(),
        Leaf<AccessCallChainNode>(),
        Leaf<ParameterReferenceNode>(),
        Leaf<StringNode>(),
        Leaf<IntegerNode>(),
        Leaf<HexIntegerNode>(),
        Leaf<BinaryIntegerNode>(),
        Leaf<OctalIntegerNode>(),
        Leaf<BooleanNode>(),
        Leaf<WordNode>(),
        Leaf<DecimalNode>(),
        Leaf<NullNode>(),
        Leaf<IdentifierNode>()
    ];

    public static IReadOnlyList<ParserNodeTraversalDescriptor> Descriptors => RegisteredDescriptors;

    public static IEnumerable<Node> EnumerateChildren(Node node)
    {
        return ResolveDescriptor(node.GetType()).Enumerate(node);
    }

    public static ParserNodeTraversalDescriptor ResolveDescriptor(Type nodeType)
    {
        foreach (var descriptor in RegisteredDescriptors)
        {
            if (descriptor.Covers(nodeType))
                return descriptor;
        }

        return new ParserNodeTraversalDescriptor(
            nodeType,
            ParserNodeTraversalMode.Unsupported,
            false,
            static _ => []);
    }

    public static IEnumerable<Node> EnumerateCteInnerExpressionsThenOuter(CteExpressionNode node)
    {
        foreach (var expression in node.InnerExpression)
            yield return expression;

        yield return node.OuterExpression;
    }

    private static ParserNodeTraversalDescriptor Children<T>(
        Func<T, IEnumerable<Node>> enumerateChildren,
        bool includeDerivedTypes = false)
        where T : Node
    {
        return new ParserNodeTraversalDescriptor(
            typeof(T),
            ParserNodeTraversalMode.Children,
            includeDerivedTypes,
            node => enumerateChildren((T)node));
    }

    private static ParserNodeTraversalDescriptor Special<T>(
        Func<T, IEnumerable<Node>> enumerateChildren,
        bool includeDerivedTypes = false)
        where T : Node
    {
        return new ParserNodeTraversalDescriptor(
            typeof(T),
            ParserNodeTraversalMode.SpecialOrder,
            includeDerivedTypes,
            node => enumerateChildren((T)node));
    }

    private static ParserNodeTraversalDescriptor Leaf<T>(bool includeDerivedTypes = false)
        where T : Node
    {
        return new ParserNodeTraversalDescriptor(
            typeof(T),
            ParserNodeTraversalMode.Leaf,
            includeDerivedTypes,
            static _ => []);
    }

    private static IEnumerable<Node> AllColumnsChildren(AllColumnsNode node)
    {
        if (node.ReplaceItems is not { Length: > 0 })
            yield break;

        foreach (var replaceItem in node.ReplaceItems)
            yield return replaceItem.Expression;
    }

    private static IEnumerable<Node> CaseChildren(CaseNode node)
    {
        yield return node.Else;

        for (var i = node.WhenThenPairs.Length - 1; i >= 0; --i)
        {
            yield return node.WhenThenPairs[i].When;
            yield return node.WhenThenPairs[i].Then;
        }
    }

    private static IEnumerable<Node> CteChildren(CteExpressionNode node)
    {
        yield return node.OuterExpression;

        foreach (var expression in node.InnerExpression)
            yield return expression;
    }

    private static IEnumerable<Node> GroupByChildren(GroupByNode node)
    {
        foreach (var field in node.Fields)
            yield return field;

        if (node.Having != null)
            yield return node.Having;
    }

    private static IEnumerable<Node> Optional(params Node?[] nodes)
    {
        foreach (var node in nodes)
            if (node != null)
                yield return node;
    }

    private static IEnumerable<Node> QueryChildren(QueryNode node)
    {
        yield return node.From;

        if (node.Where != null)
            yield return node.Where;

        yield return node.Select;

        if (node.Take != null)
            yield return node.Take;

        if (node.Skip != null)
            yield return node.Skip;

        if (node.GroupBy != null)
            yield return node.GroupBy;

        if (node.Window != null)
            yield return node.Window;

        if (node.Qualify != null)
            yield return node.Qualify;

        if (node.OrderBy != null)
            yield return node.OrderBy;
    }

    private static IEnumerable<Node> TranslatedSetOperatorChildren(TranslatedSetOperatorNode node)
    {
        foreach (var createTableNode in node.CreateTableNodes)
            yield return createTableNode;

        yield return node.FQuery;
        yield return node.SQuery;
    }

    private static IEnumerable<Node> ValuesChildren(ValuesFromNode node)
    {
        foreach (var row in node.Rows)
        foreach (var field in row.Fields)
            yield return field.Expression;
    }

    private static IEnumerable<Node> UnpivotChildren(UnpivotFromNode node)
    {
        yield return node.Source;

        foreach (var entry in node.Entries)
            yield return entry.Expression;

        foreach (var keepField in node.KeepFields)
            yield return keepField;
    }

    private static IEnumerable<Node> WindowSpecificationChildren(WindowSpecificationNode node)
    {
        foreach (var field in node.PartitionFields)
            yield return field;

        foreach (var field in node.OrderByFields)
            yield return field;

        if (node.Frame != null)
            yield return node.Frame;
    }
}
