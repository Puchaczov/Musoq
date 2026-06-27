using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning.Cardinality;

internal static class NestedLoopJoinRiskAdvisor
{
    private const long HighRiskComparisonThreshold = 1_000_000L;

    public static PlanningDecision CreateRiskDecision(
        JoinKind kind,
        PhysicalNode left,
        PhysicalNode right,
        IReadOnlyList<CardinalityFact>? cardinalityFacts)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var leftRows = TryResolveSourceRows(left, cardinalityFacts);
        var rightRows = TryResolveSourceRows(right, cardinalityFacts);
        var (outcome, confidence, reason) = AssessRisk(leftRows, rightRows);

        return new PlanningDecision(
            PlanningDecisionCategory.JoinStrategy,
            "NestedLoopCardinalityRisk",
            kind.ToString(),
            outcome,
            confidence,
            reason);
    }

    private static (string Outcome, PlanningConfidence Confidence, string Reason) AssessRisk(
        long? leftRows,
        long? rightRows)
    {
        if (leftRows is not { } left || rightRows is not { } right)
            return (
                "UnknownRisk",
                PlanningConfidence.Medium,
                "Nested-loop join runs O(n*m) over inputs with unknown cardinality, which can become a performance cliff on large sources.");

        if (ExceedsHighRiskThreshold(left, right))
            return (
                "HighRisk",
                PlanningConfidence.High,
                $"Nested-loop join evaluates roughly {left} x {right} row comparisons, which is expensive at this scale.");

        return (
            "LowRisk",
            PlanningConfidence.High,
            $"Nested-loop join over small inputs ({left} x {right} row comparisons) stays inexpensive.");
    }

    private static bool ExceedsHighRiskThreshold(long left, long right)
    {
        if (left == 0 || right == 0)
            return false;

        return left > HighRiskComparisonThreshold / right;
    }

    private static long? TryResolveSourceRows(
        PhysicalNode node,
        IReadOnlyList<CardinalityFact>? cardinalityFacts)
    {
        var scan = ResolveSourceScan(node);
        if (scan == null || string.IsNullOrWhiteSpace(scan.SourceContextId))
            return null;

        return CardinalityFactAdvisor.TryResolveComparableRows(
            cardinalityFacts,
            $"source:{scan.SourceContextId}",
            out var rows)
            ? rows
            : null;
    }

    private static PhysicalSchemaScanNode? ResolveSourceScan(PhysicalNode node)
    {
        while (node is PhysicalFilterNode filter)
            node = filter.Input;

        return node as PhysicalSchemaScanNode;
    }
}
