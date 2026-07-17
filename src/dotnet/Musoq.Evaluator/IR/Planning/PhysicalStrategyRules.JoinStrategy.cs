using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PhysicalStrategyRules
{
    public static JoinStrategyDecision ChooseJoinStrategy(
        JoinKind kind,
        IrExpression predicate,
        PhysicalNode left,
        PhysicalNode right,
        CompilationOptions compilationOptions,
        IReadOnlyList<CardinalityFact>? cardinalityFacts = null)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(compilationOptions);
        if (kind is JoinKind.LeftSemi or JoinKind.LeftAntiSemi or JoinKind.LeftMark or JoinKind.LeftSingle)
        {
            var boundedHash = TryDecomposeHashJoin(kind, predicate, left, right, cardinalityFacts);
            var boundedRange = TryDecomposeSortMergeJoin(kind, predicate, left, right);
            if (boundedRange != null)
            {
                return JoinStrategyDecision.SortMerge(
                    boundedRange,
                    kind switch
                    {
                        JoinKind.LeftSingle => "Range-single join selected because scalar cardinality can be enforced over one indexed comparable range within its equality partition when present.",
                        JoinKind.LeftAntiSemi => "Range anti-semi join selected because the indexed comparable range can prove absence within its equality partition.",
                        JoinKind.LeftSemi => "Range semi-join selected because the indexed comparable range can stop after the first match within its equality partition.",
                        _ => "Range-mark join selected because predicate truth can stop after the first indexed comparable-range match within its equality partition."
                    });
            }

            if (boundedHash != null)
            {
                return JoinStrategyDecision.Hash(
                    boundedHash,
                    kind == JoinKind.LeftSingle
                        ? $"Hash-single join selected because scalar cardinality enforcement requires its equi-key probe. {boundedHash.BuildSideReason}"
                        : $"Bounded hash join selected because predicate truth can be resolved within one equi-key bucket. {boundedHash.BuildSideReason}");
            }

            return JoinStrategyDecision.NestedLoop(
                kind == JoinKind.LeftSingle
                    ? "Bounded scalar lowering requires an equi-key or one comparable range predicate; no eligible index key was found."
                    : "Bounded mark lowering requires an equi-key or one comparable range predicate; no eligible index key was found.");
        }

        if (compilationOptions.UseHashJoin)
        {
            var hashJoin = TryDecomposeHashJoin(kind, predicate, left, right, cardinalityFacts);
            if (hashJoin != null)
            {
                return JoinStrategyDecision.Hash(
                    hashJoin,
                    $"Hash join selected because at least one equi key pair was found. {hashJoin.BuildSideReason}");
            }
        }

        if (compilationOptions.UseSortMergeJoin)
        {
            var sortMergeJoin = TryDecomposeSortMergeJoin(kind, predicate, left, right);
            if (sortMergeJoin != null)
                return JoinStrategyDecision.SortMerge(sortMergeJoin, "Sort-merge join selected because one comparable range predicate was found.");
        }

        return JoinStrategyDecision.NestedLoop(CreateNestedLoopReason(kind, compilationOptions));
    }

    private static string CreateNestedLoopReason(JoinKind kind, CompilationOptions compilationOptions)
    {
        if (kind is JoinKind.AsofInner or JoinKind.AsofLeft)
            return "ASOF join semantics require nested-loop evaluation.";

        if (kind == JoinKind.Cross)
            return "CROSS JOIN semantics require nested-loop Cartesian evaluation.";

        if (compilationOptions is { UseHashJoin: false, UseSortMergeJoin: false })
            return "Hash and sort-merge joins are disabled by compilation options.";

        if (!compilationOptions.UseHashJoin)
            return "Hash join is disabled and no sort-merge strategy was eligible.";

        if (!compilationOptions.UseSortMergeJoin)
            return "No hash-join equi key pair was found and sort-merge join is disabled.";

        return "No hash or sort-merge strategy was eligible.";
    }
}
