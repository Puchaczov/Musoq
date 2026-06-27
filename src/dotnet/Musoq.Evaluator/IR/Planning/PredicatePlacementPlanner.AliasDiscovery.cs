using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicatePlacementPlanner
{
    private static bool IsPushedPredicate(
        string sourceContextId,
        string predicateText,
        IReadOnlyDictionary<string, IReadOnlySet<string>> pushedPredicatesBySourceId)
    {
        return pushedPredicatesBySourceId.TryGetValue(sourceContextId, out var pushedPredicates) &&
               pushedPredicates.Contains(predicateText);
    }

    private static SourcePlanProperties? TryResolveUniqueSource(
        IReadOnlyDictionary<string, SourcePlanProperties[]> sourcesByAlias,
        string alias)
    {
        if (!sourcesByAlias.TryGetValue(alias, out var sources) || sources.Length != 1)
            return null;

        return sources[0];
    }

    private static bool HasAmbiguousSourceAlias(
        IReadOnlyDictionary<string, SourcePlanProperties[]> sourcesByAlias,
        string alias)
    {
        return sourcesByAlias.TryGetValue(alias, out var sources) && sources.Length > 1;
    }

    private static LogicalNode[] AppendAncestor(IReadOnlyList<LogicalNode> ancestors, LogicalNode node)
    {
        var nextAncestors = new LogicalNode[ancestors.Count + 1];
        for (var index = 0; index < ancestors.Count; index++)
            nextAncestors[index] = ancestors[index];

        nextAncestors[^1] = node;
        return nextAncestors;
    }

    private static JoinNode? FindNearestJoinBoundary(IReadOnlyList<LogicalNode> ancestors)
    {
        for (var index = ancestors.Count - 1; index >= 0; index--)
        {
            if (ancestors[index] is JoinNode join)
                return join;
        }

        return null;
    }

    private static JoinNode? FindJoinBoundary(LogicalNode node)
    {
        if (node is JoinNode join)
            return join;

        foreach (var child in node.Children)
        {
            var childJoin = FindJoinBoundary(child);
            if (childJoin is not null)
                return childJoin;
        }

        return null;
    }

    private static ApplyNode? FindApplyBoundary(LogicalNode node)
    {
        if (node is ApplyNode apply)
            return apply;

        foreach (var child in node.Children)
        {
            var childApply = FindApplyBoundary(child);
            if (childApply is not null)
                return childApply;
        }

        return null;
    }

    private static List<JoinNode> CollectJoinBoundaries(LogicalNode node)
    {
        var joins = new List<JoinNode>();
        AddJoinBoundaries(node, joins);
        return joins;
    }

    private static List<ApplyNode> CollectApplyBoundaries(LogicalNode node)
    {
        var applies = new List<ApplyNode>();
        AddApplyBoundaries(node, applies);
        return applies;
    }

    private static void AddJoinBoundaries(LogicalNode node, List<JoinNode> joins)
    {
        if (node is JoinNode join)
            joins.Add(join);

        foreach (var child in node.Children)
            AddJoinBoundaries(child, joins);
    }

    private static void AddApplyBoundaries(LogicalNode node, List<ApplyNode> applies)
    {
        if (node is ApplyNode apply)
            applies.Add(apply);

        foreach (var child in node.Children)
            AddApplyBoundaries(child, applies);
    }

    private static JoinNode? FindRelatedJoinBoundary(IReadOnlyList<JoinNode> joinBoundaries, string[] aliases)
    {
        for (var index = joinBoundaries.Count - 1; index >= 0; index--)
        {
            var join = joinBoundaries[index];
            var leftAliases = CollectProducedAliases(join.Left);
            var rightAliases = CollectProducedAliases(join.Right);
            var referencesLeft = aliases.Any(leftAliases.Contains);
            var referencesRight = aliases.Any(rightAliases.Contains);

            if (join.Kind != JoinKind.Inner && (referencesLeft || referencesRight))
                return join;

            if (join.Kind == JoinKind.Inner && aliases.Length > 1 && referencesLeft && referencesRight)
                return join;
        }

        return null;
    }

    private static ApplyNode? FindRelatedApplyBoundary(IReadOnlyList<ApplyNode> applyBoundaries, string[] aliases)
    {
        for (var index = applyBoundaries.Count - 1; index >= 0; index--)
        {
            var apply = applyBoundaries[index];
            var leftAliases = CollectProducedAliases(apply.Left);
            var rightAliases = CollectProducedAliases(apply.Right);

            if (aliases.Any(leftAliases.Contains) && aliases.Any(rightAliases.Contains))
                return apply;
        }

        return null;
    }

    private static HashSet<string> CollectProducedAliases(LogicalNode node)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddProducedAliases(node, aliases);
        return aliases;
    }

    private static void AddProducedAliases(LogicalNode node, HashSet<string> aliases)
    {
        switch (node)
        {
            case SchemaScanNode scan:
                aliases.Add(scan.Alias);
                break;
            case InterpretSourceNode interpret:
                aliases.Add(interpret.Alias);
                break;
            case PropertySourceNode property:
                aliases.Add(property.Alias);
                break;
            case AccessMethodSourceNode accessMethod:
                aliases.Add(accessMethod.Alias);
                break;
            case CteRefNode cteRef:
                aliases.Add(cteRef.Alias);
                break;
            case ValuesScanNode values:
                aliases.Add(values.Alias);
                break;
        }

        foreach (var child in node.Children)
            AddProducedAliases(child, aliases);
    }

    private static bool IsSubset(IEnumerable<string> aliases, IReadOnlySet<string> scopeAliases)
    {
        return aliases.All(scopeAliases.Contains);
    }
}
