using System;
using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Analyzes the query tree to identify common subexpressions that can be cached.
///     This visitor counts occurrences of each expression and determines which ones
///     appear multiple times and are therefore candidates for caching.
/// </summary>
public class CommonSubexpressionAnalysisVisitor : NoOpExpressionVisitor
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

    public override void Visit(StarNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(FSlashNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(ModuloNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(AddNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(HyphenNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(BitwiseAndNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(BitwiseOrNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(BitwiseXorNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(LeftShiftNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(RightShiftNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(ArrayIndexNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(AndNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(OrNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(EqualityNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(GreaterOrEqualNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(LessOrEqualNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(GreaterNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(LessNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(DiffNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(NotNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(LikeNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(RLikeNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(InNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(BetweenNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(ContainsNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(AccessMethodNode node)
    {
        TrackExpression(node);

        if (IsNonDeterministicFunction(node)) MarkNonCacheable(node.Id);
    }

    public override void Visit(IsNullNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(AccessRefreshAggregationScoreNode node)
    {
        TrackExpression(node);
        MarkNonCacheable(node.Id);
    }

    public override void Visit(PropertyValueNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(DotNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(AccessCallChainNode node)
    {
        TrackExpression(node);
    }

    public override void Visit(CaseNode node)
    {
        TrackExpression(node);
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
