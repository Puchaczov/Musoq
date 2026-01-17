using System;
using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Analyzes the query tree to identify common subexpressions that can be cached.
///     This visitor counts occurrences of each expression and determines which ones
///     appear multiple times and are therefore candidates for caching.
/// </summary>
public class CommonSubexpressionAnalysisVisitor : IExpressionVisitor
{
    private readonly Dictionary<string, Node> _expressionNodes = new();

    private readonly Dictionary<string, int> _expressionOccurrences = new();
    private readonly HashSet<string> _nonCacheableExpressions = [];
    private readonly HashSet<string> _nonDeterministicFunctions;

    private readonly HashSet<string> _seenInSafeContext = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="CommonSubexpressionAnalysisVisitor" /> class.
    /// </summary>
    /// <param name="nonDeterministicFunctions">
    ///     Set of function names that are non-deterministic and should never be cached.
    ///     These are typically discovered by scanning library assemblies for methods marked with [NonDeterministic] attribute.
    /// </param>
    public CommonSubexpressionAnalysisVisitor(HashSet<string> nonDeterministicFunctions)
    {
        _nonDeterministicFunctions = nonDeterministicFunctions ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     When true, expressions are in a "pass-through" unsafe context (e.g., CASE WHEN).
    ///     CSE variables computed earlier can still be used via method parameters.
    ///     Expressions appearing ONLY in this context should not be cached.
    /// </summary>
    public bool InPassThroughUnsafeContext { get; set; }

    /// <summary>
    ///     When true, expressions are in a "separate-scope" unsafe context (e.g., ORDER BY, GROUP BY, HAVING).
    ///     CSE variables cannot be used because they're in a different execution scope.
    ///     Expressions appearing here must be marked non-cacheable.
    /// </summary>
    public bool InSeparateScopeContext { get; set; }

    public void Visit(Node node)
    {
    }

    public void Visit(DescNode node)
    {
    }

    public void Visit(StarNode node)
    {
        TrackExpression(node);
    }

    public void Visit(FSlashNode node)
    {
        TrackExpression(node);
    }

    public void Visit(ModuloNode node)
    {
        TrackExpression(node);
    }

    public void Visit(AddNode node)
    {
        TrackExpression(node);
    }

    public void Visit(HyphenNode node)
    {
        TrackExpression(node);
    }

    public void Visit(AndNode node)
    {
        TrackExpression(node);
    }

    public void Visit(OrNode node)
    {
        TrackExpression(node);
    }

    public void Visit(ShortCircuitingNodeLeft node)
    {
    }

    public void Visit(ShortCircuitingNodeRight node)
    {
    }

    public void Visit(EqualityNode node)
    {
        TrackExpression(node);
    }

    public void Visit(GreaterOrEqualNode node)
    {
        TrackExpression(node);
    }

    public void Visit(LessOrEqualNode node)
    {
        TrackExpression(node);
    }

    public void Visit(GreaterNode node)
    {
        TrackExpression(node);
    }

    public void Visit(LessNode node)
    {
        TrackExpression(node);
    }

    public void Visit(DiffNode node)
    {
        TrackExpression(node);
    }

    public void Visit(NotNode node)
    {
        TrackExpression(node);
    }

    public void Visit(LikeNode node)
    {
        TrackExpression(node);
    }

    public void Visit(RLikeNode node)
    {
        TrackExpression(node);
    }

    public void Visit(InNode node)
    {
        TrackExpression(node);
    }

    public void Visit(FieldNode node)
    {
    }

    public void Visit(FieldOrderedNode node)
    {
    }

    public void Visit(StringNode node)
    {
    }

    public void Visit(DecimalNode node)
    {
    }

    public void Visit(IntegerNode node)
    {
    }

    public void Visit(HexIntegerNode node)
    {
    }

    public void Visit(BinaryIntegerNode node)
    {
    }

    public void Visit(OctalIntegerNode node)
    {
    }

    public void Visit(BooleanNode node)
    {
    }

    public void Visit(WordNode node)
    {
    }

    public void Visit(NullNode node)
    {
    }

    public void Visit(ContainsNode node)
    {
        TrackExpression(node);
    }

    public void Visit(AccessMethodNode node)
    {
        TrackExpression(node);

        if (IsNonDeterministicFunction(node)) MarkNonCacheable(node.Id);
    }

    public void Visit(AccessRawIdentifierNode node)
    {
    }

    public void Visit(IsNullNode node)
    {
        TrackExpression(node);
    }

    public void Visit(AccessRefreshAggregationScoreNode node)
    {
        TrackExpression(node);
        MarkNonCacheable(node.Id);
    }

    public void Visit(AccessColumnNode node)
    {
    }

    public void Visit(AllColumnsNode node)
    {
    }

    public void Visit(IdentifierNode node)
    {
    }

    public void Visit(AccessObjectArrayNode node)
    {
    }

    public void Visit(AccessObjectKeyNode node)
    {
    }

    public void Visit(PropertyValueNode node)
    {
        TrackExpression(node);
    }

    public void Visit(DotNode node)
    {
        TrackExpression(node);
    }

    public void Visit(AccessCallChainNode node)
    {
        TrackExpression(node);
    }

    public void Visit(ArgsListNode node)
    {
    }

    public void Visit(SelectNode node)
    {
    }

    public void Visit(GroupSelectNode node)
    {
    }

    public void Visit(WhereNode node)
    {
    }

    public void Visit(GroupByNode node)
    {
    }

    public void Visit(HavingNode node)
    {
    }

    public void Visit(SkipNode node)
    {
    }

    public void Visit(TakeNode node)
    {
    }

    public void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
    }

    public void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
    }

    public void Visit(SchemaFromNode node)
    {
    }

    public void Visit(AliasedFromNode node)
    {
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
    }

    public void Visit(InMemoryTableFromNode node)
    {
    }

    public void Visit(JoinFromNode node)
    {
    }

    public void Visit(ApplyFromNode node)
    {
    }

    public void Visit(ExpressionFromNode node)
    {
    }

    public void Visit(SchemaMethodFromNode node)
    {
    }

    public void Visit(PropertyFromNode node)
    {
    }

    public void Visit(AccessMethodFromNode node)
    {
    }

    public void Visit(CreateTransformationTableNode node)
    {
    }

    public void Visit(RenameTableNode node)
    {
    }

    public void Visit(TranslatedSetTreeNode node)
    {
    }

    public void Visit(IntoNode node)
    {
    }

    public void Visit(QueryScope node)
    {
    }

    public void Visit(ShouldBePresentInTheTable node)
    {
    }

    public void Visit(TranslatedSetOperatorNode node)
    {
    }

    public void Visit(QueryNode node)
    {
    }

    public void Visit(InternalQueryNode node)
    {
    }

    public void Visit(RootNode node)
    {
    }

    public void Visit(SingleSetNode node)
    {
    }

    public void Visit(UnionNode node)
    {
    }

    public void Visit(UnionAllNode node)
    {
    }

    public void Visit(ExceptNode node)
    {
    }

    public void Visit(RefreshNode node)
    {
    }

    public void Visit(IntersectNode node)
    {
    }

    public void Visit(PutTrueNode node)
    {
    }

    public void Visit(MultiStatementNode node)
    {
    }

    public void Visit(StatementsArrayNode node)
    {
    }

    public void Visit(StatementNode node)
    {
    }

    public void Visit(CteExpressionNode node)
    {
    }

    public void Visit(CteInnerExpressionNode node)
    {
    }

    public void Visit(JoinNode node)
    {
    }

    public void Visit(ApplyNode node)
    {
    }

    public void Visit(OrderByNode node)
    {
    }

    public void Visit(CreateTableNode node)
    {
    }

    public void Visit(CoupleNode node)
    {
    }

    public void Visit(CaseNode node)
    {
        TrackExpression(node);
    }

    public void Visit(WhenNode node)
    {
    }

    public void Visit(ThenNode node)
    {
    }

    public void Visit(ElseNode node)
    {
    }

    public void Visit(FieldLinkNode node)
    {
    }

    /// <summary>
    ///     Gets the map of expression IDs to their cache slot indices.
    ///     Only expressions that appear more than once and are cacheable are included.
    /// </summary>
    public IReadOnlyDictionary<string, int> GetCacheSlotMap()
    {
        var cacheSlotMap = new Dictionary<string, int>();
        var slotIndex = 0;

        foreach (var (expressionId, count) in _expressionOccurrences)
        {
            if (count <= 1 || _nonCacheableExpressions.Contains(expressionId))
                continue;

            if (_expressionNodes.TryGetValue(expressionId, out var node) && IsWorthCaching(node))
                cacheSlotMap[expressionId] = slotIndex++;
        }

        return cacheSlotMap;
    }

    private void TrackExpression(Node node)
    {
        var id = node.Id;

        if (_expressionOccurrences.TryAdd(id, 0)) _expressionNodes[id] = node;

        _expressionOccurrences[id]++;

        if (!InPassThroughUnsafeContext) _seenInSafeContext.Add(id);

        if (InSeparateScopeContext ||
            (InPassThroughUnsafeContext && !_seenInSafeContext.Contains(id)))
            MarkNonCacheable(id);
    }

    private void MarkNonCacheable(string expressionId)
    {
        _nonCacheableExpressions.Add(expressionId);
    }

    private static bool IsWorthCaching(Node node)
    {
        switch (node)
        {
            case IntegerNode or DecimalNode or StringNode or BooleanNode or NullNode or
                HexIntegerNode or BinaryIntegerNode or OctalIntegerNode or WordNode:
                return false;
            case AccessColumnNode columnNode:
            {
                var returnType = columnNode.ReturnType;
                return returnType != null &&
                       returnType != typeof(void) &&
                       (returnType.IsValueType || returnType == typeof(string));
            }
            case AccessObjectArrayNode or AccessObjectKeyNode:
            case IdentifierNode or AccessRawIdentifierNode:
                return false;
            default:
                return true;
        }
    }

    private bool IsNonDeterministicFunction(AccessMethodNode node)
    {
        return _nonDeterministicFunctions.Contains(node.Name);
    }
}