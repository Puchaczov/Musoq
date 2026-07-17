using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;
using Musoq.Evaluator.IR.Planning.SourcePlanning;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PhysicalStrategyRules
{
    private static HashJoinDecomposition? TryDecomposeHashJoin(
        JoinKind kind,
        IrExpression predicate,
        PhysicalNode left,
        PhysicalNode right,
        IReadOnlyList<CardinalityFact>? cardinalityFacts)
    {
        if (kind is JoinKind.AsofInner or JoinKind.AsofLeft or JoinKind.Cross)
            return null;

        var leftKeys = new List<IrExpression>();
        var rightKeys = new List<IrExpression>();
        var residuals = new List<IrExpression>();
        var leftAliases = CollectAliases(left);
        var rightAliases = CollectAliases(right);

        CollectHashJoinConditions(
            predicate,
            leftAliases,
            rightAliases,
            leftKeys,
            rightKeys,
            residuals,
            kind is JoinKind.LeftSemi or JoinKind.LeftAntiSemi or JoinKind.LeftMark);
        if (leftKeys.Count == 0)
            return null;

        var residual = CreateResidualPredicate(residuals);
        if (ShouldBuildHashJoinOnLeft(kind, left, right, cardinalityFacts, out var buildSideReason))
            return new HashJoinDecomposition([.. leftKeys], [.. rightKeys], residual, buildSideReason);

        return new HashJoinDecomposition([.. rightKeys], [.. leftKeys], residual, buildSideReason);
    }

    private static bool ShouldBuildHashJoinOnLeft(
        JoinKind kind,
        PhysicalNode left,
        PhysicalNode right,
        IReadOnlyList<CardinalityFact>? cardinalityFacts,
        out string reason)
    {
        reason = string.Empty;

        if (kind == JoinKind.RightOuter)
        {
            reason = "Right outer join keeps the left side as the hash build side to preserve join semantics.";
            return true;
        }

        if (kind is JoinKind.LeftSemi or JoinKind.LeftAntiSemi or JoinKind.LeftMark or JoinKind.LeftSingle)
        {
            reason = "Semi, anti-semi, mark, and single joins keep the right side as the hash build side to preserve join semantics.";
            return false;
        }

        if (SourceCardinalityHashBuildAdvisor.TryChooseBuildSide(
                kind,
                left,
                right,
                cardinalityFacts,
                out var buildOnLeft,
                out reason))
        {
            return buildOnLeft;
        }

        if (kind == JoinKind.Inner && left is PhysicalCteRefNode && right is PhysicalSchemaScanNode)
        {
            reason = "Runtime v2 build-side selector keeps the CTE side as the hash build side for this CTE-to-source inner join.";
            return true;
        }

        reason = kind is JoinKind.Inner or JoinKind.FullOuter
            ? $"{reason} Runtime v2 build-side selector used the stable right-side tie-breaker."
            : "Runtime v2 build-side selector used the right side for this join kind.";
        return false;
    }

    private static void CollectHashJoinConditions(
        IrExpression expression,
        HashSet<string> leftAliases,
        HashSet<string> rightAliases,
        List<IrExpression> leftKeys,
        List<IrExpression> rightKeys,
        List<IrExpression> residuals,
        bool allowConstantKeys)
    {
        switch (expression)
        {
            case BinaryOp { Kind: BinaryOpKind.And } and:
                CollectHashJoinConditions(and.Left, leftAliases, rightAliases, leftKeys, rightKeys, residuals, allowConstantKeys);
                CollectHashJoinConditions(and.Right, leftAliases, rightAliases, leftKeys, rightKeys, residuals, allowConstantKeys);
                return;
            case BinaryOp { Kind: BinaryOpKind.Equal } equality
                when TryMapEqualityCondition(equality, leftAliases, rightAliases, leftKeys, rightKeys, allowConstantKeys):
                return;
            default:
                residuals.Add(expression);
                return;
        }
    }

    private static IrExpression? CreateResidualPredicate(List<IrExpression> residuals)
    {
        if (residuals.Count == 0)
            return null;

        var residual = residuals[0];
        for (var i = 1; i < residuals.Count; i++)
        {
            residual = new BinaryOp(
                BinaryOpKind.And,
                residual,
                residuals[i],
                typeof(bool));
        }

        return residual;
    }
}
