using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static class SourceCardinalityHashBuildAdvisor
{
    public static bool TryChooseBuildSide(
        JoinKind kind,
        PhysicalNode left,
        PhysicalNode right,
        IReadOnlyList<CardinalityFact>? cardinalityFacts,
        out bool buildOnLeft,
        out string reason)
    {
        buildOnLeft = false;

        if (kind is not (JoinKind.Inner or JoinKind.FullOuter))
        {
            reason = "Cardinality build-side selection applies only to inner joins and full outer joins.";
            return false;
        }

        if (cardinalityFacts == null || cardinalityFacts.Count == 0)
        {
            reason = "No cardinality facts were available for hash build-side selection.";
            return false;
        }

        if (!TryResolveSimpleSourceScan(left, out var leftScan) ||
            !TryResolveSimpleSourceScan(right, out var rightScan) ||
            string.IsNullOrWhiteSpace(leftScan.SourceContextId) ||
            string.IsNullOrWhiteSpace(rightScan.SourceContextId))
        {
            reason = "Cardinality build-side selection applies only to simple source-scan inner joins and full outer joins.";
            return false;
        }

        if (!CardinalityFactAdvisor.TryResolveComparableRows(cardinalityFacts, $"source:{leftScan.SourceContextId}", out var leftRows) ||
            !CardinalityFactAdvisor.TryResolveComparableRows(cardinalityFacts, $"source:{rightScan.SourceContextId}", out var rightRows))
        {
            reason = "Cardinality facts were missing, unknown, or too low-confidence for hash build-side selection.";
            return false;
        }

        if (IsClearlySmaller(leftRows, rightRows))
        {
            buildOnLeft = true;
            reason = $"Cardinality fact selected the left source as hash build side ({leftRows} row(s) vs {rightRows} row(s)).";
            return true;
        }

        if (IsClearlySmaller(rightRows, leftRows))
        {
            buildOnLeft = false;
            reason = $"Cardinality fact selected the right source as hash build side ({rightRows} row(s) vs {leftRows} row(s)).";
            return true;
        }

        reason = $"Cardinality facts were too close for hash build-side selection ({leftRows} row(s) vs {rightRows} row(s)).";
        return false;
    }

    private static bool TryResolveSimpleSourceScan(
        PhysicalNode node,
        out PhysicalSchemaScanNode scan)
    {
        while (node is PhysicalFilterNode filter) node = filter.Input;

        var schemaScan = node as PhysicalSchemaScanNode;
        scan = schemaScan ?? null!;
        return schemaScan != null;
    }

    private static bool IsClearlySmaller(long candidate, long other)
    {
        return candidate < other && candidate <= other / 2;
    }
}
