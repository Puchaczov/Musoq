using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourcePredicateTransferBoundary(
    PredicatePlacementOrigin Origin, PredicateMovementSide Side, JoinKind JoinKind);

internal static class SourcePredicateTransferBoundaryPlanner
{
    // WHERE predicates must remain after null extension. Source IDs, rather than
    // aliases, distinguish scans in nested scopes and reused CTEs.
    public static IReadOnlyDictionary<string, SourcePredicateTransferBoundary> Plan(LogicalNode? root)
    {
        var result = new Dictionary<string, SourcePredicateTransferBoundary>(StringComparer.Ordinal);
        if (root != null) Visit(root, result);
        return result;
    }

    private static void Visit(LogicalNode node, Dictionary<string, SourcePredicateTransferBoundary> result)
    {
        if (node is JoinNode join)
        {
            if (join.Kind is JoinKind.RightOuter or JoinKind.FullOuter)
                Mark(join.Left, new(PredicatePlacementOrigin.Where, PredicateMovementSide.Left, join.Kind), result);
            if (join.Kind is JoinKind.LeftOuter or JoinKind.FullOuter or JoinKind.AsofLeft)
                Mark(join.Right, new(PredicatePlacementOrigin.Where, PredicateMovementSide.Right, join.Kind), result);
        }
        foreach (var child in node.Children) Visit(child, result);
    }

    private static void Mark(LogicalNode node, SourcePredicateTransferBoundary boundary,
        Dictionary<string, SourcePredicateTransferBoundary> result)
    {
        if (node is SchemaScanNode scan && !string.IsNullOrWhiteSpace(scan.SourceContextId))
            result[scan.SourceContextId] = boundary;
        foreach (var child in node.Children) Mark(child, boundary, result);
    }
}
