using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceTransferLifetimePlanner
{
    public static IReadOnlyDictionary<string, SourceTransferLifetimePlan> Plan(LogicalNode logicalPlan)
    {
        ArgumentNullException.ThrowIfNull(logicalPlan);

        var parents = CreateParentIndex(logicalPlan);
        var plans = new Dictionary<string, SourceTransferLifetimePlan>(StringComparer.Ordinal);
        foreach (var scan in FindSchemaScans(logicalPlan))
        {
            if (string.IsNullOrWhiteSpace(scan.SourceContextId))
                continue;

            plans[scan.SourceContextId] = Classify(scan, parents);
        }

        return plans;
    }

    private static SourceTransferLifetimePlan Classify(
        SchemaScanNode scan,
        IReadOnlyDictionary<LogicalNode, List<LogicalNode>> parents)
    {
        LogicalNode current = scan;
        var visited = new HashSet<LogicalNode>(ReferenceEqualityComparer.Instance);
        while (visited.Add(current))
        {
            if (!parents.TryGetValue(current, out var currentParents) || currentParents.Count == 0)
            {
                return SourceTransferLifetimePlan.Escapes(
                    scan.SourceContextId!,
                    "The source row reaches a plan root before a row-shape replacement.");
            }

            if (currentParents.Count > 1)
            {
                return SourceTransferLifetimePlan.Escapes(
                    scan.SourceContextId!,
                    "The source row has multiple logical consumers before a row-shape replacement.");
            }

            var parent = currentParents[0];
            if (parent is ProjectNode)
            {
                return SourceTransferLifetimePlan.ScanLocal(
                    scan.SourceContextId!,
                    "Projection replaces the source carrier before any retaining boundary.");
            }

            if (parent is AggregateNode)
            {
                return SourceTransferLifetimePlan.ScanLocal(
                    scan.SourceContextId!,
                    "Aggregation replaces the source carrier before any retaining boundary.");
            }

            if (IsStreamingTransparent(parent))
            {
                current = parent;
                continue;
            }

            return SourceTransferLifetimePlan.Escapes(
                scan.SourceContextId!,
                $"{parent.GetType().Name} retains or crosses the source row before a row-shape replacement.");
        }

        return SourceTransferLifetimePlan.Escapes(
            scan.SourceContextId!,
            "A logical-plan cycle was encountered before a row-shape replacement.");
    }

    private static bool IsStreamingTransparent(LogicalNode node)
    {
        return node is FilterNode or HavingFilterNode or QualifyFilterNode or SkipNode or TakeNode;
    }

    private static Dictionary<LogicalNode, List<LogicalNode>> CreateParentIndex(LogicalNode root)
    {
        var parents = new Dictionary<LogicalNode, List<LogicalNode>>(ReferenceEqualityComparer.Instance);
        var visited = new HashSet<LogicalNode>(ReferenceEqualityComparer.Instance);
        AddParents(root, parents, visited);
        return parents;
    }

    private static void AddParents(
        LogicalNode node,
        IDictionary<LogicalNode, List<LogicalNode>> parents,
        ISet<LogicalNode> visited)
    {
        if (!visited.Add(node))
            return;

        foreach (var child in node.Children)
        {
            if (!parents.TryGetValue(child, out var childParents))
            {
                childParents = [];
                parents[child] = childParents;
            }

            childParents.Add(node);
            AddParents(child, parents, visited);
        }
    }

    private static IEnumerable<SchemaScanNode> FindSchemaScans(LogicalNode node)
    {
        if (node is SchemaScanNode scan)
            yield return scan;

        foreach (var child in node.Children)
        foreach (var descendant in FindSchemaScans(child))
            yield return descendant;
    }
}

internal sealed record SourceTransferLifetimePlan(
    string SourceContextId,
    SourceRowLifetime Lifetime,
    string Reason)
{
    public static SourceTransferLifetimePlan ScanLocal(string sourceContextId, string reason)
    {
        return new SourceTransferLifetimePlan(sourceContextId, SourceRowLifetime.ScanLocal, reason);
    }

    public static SourceTransferLifetimePlan Escapes(string sourceContextId, string reason)
    {
        return new SourceTransferLifetimePlan(sourceContextId, SourceRowLifetime.EscapesScan, reason);
    }
}
