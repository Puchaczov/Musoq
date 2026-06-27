using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static T? FindAsOfMatch<T>(
        IEnumerable<T> candidates,
        Func<T, bool>? equalityPredicate,
        Func<T, object?> keySelector,
        object? probeValue,
        BinaryOpKind comparisonKind)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(keySelector);
        if (probeValue is null)
            return null;

        T? bestCandidate = null;
        object? bestKey = null;

        foreach (var candidate in candidates)
        {
            if (equalityPredicate is not null && !equalityPredicate(candidate))
                continue;

            var candidateKey = keySelector(candidate);

            if (candidateKey is null)
                continue;

            if (!MatchesAsOfComparison(probeValue, candidateKey, comparisonKind))
                continue;

            if (bestCandidate is null ||
                bestKey is null ||
                IsBetterAsOfCandidate(candidateKey, bestKey, comparisonKind))
            {
                bestCandidate = candidate;
                bestKey = candidateKey;
            }
        }

        return bestCandidate;
    }

    public static T? FindAsOfMatch<T, TTie>(
        IEnumerable<T> candidates,
        Func<T, bool>? equalityPredicate,
        Func<T, object?> keySelector,
        object? probeValue,
        BinaryOpKind comparisonKind,
        Func<T, TTie> tieBreakKeySelector,
        bool tieBreakDescending,
        NullOrdering tieBreakNullOrdering)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(tieBreakKeySelector);
        if (probeValue is null)
            return null;

        var tieBreakComparer = AsOfTieBreakComparerFactory.Create<TTie>(tieBreakDescending, tieBreakNullOrdering);
        T? bestCandidate = null;
        object? bestKey = null;
        TTie? bestTieBreakKey = default;
        var hasBestTieBreakKey = false;

        foreach (var candidate in candidates)
        {
            if (equalityPredicate is not null && !equalityPredicate(candidate))
                continue;

            var candidateKey = keySelector(candidate);

            if (candidateKey is null)
                continue;

            if (!MatchesAsOfComparison(probeValue, candidateKey, comparisonKind))
                continue;

            var candidateTieBreakKey = tieBreakKeySelector(candidate);
            if (bestCandidate is null ||
                bestKey is null ||
                IsBetterAsOfCandidate(candidateKey, bestKey, comparisonKind) ||
                (CompareAsOfValues(candidateKey, bestKey) == 0 &&
                 (!hasBestTieBreakKey || tieBreakComparer.Compare(candidateTieBreakKey, bestTieBreakKey) < 0)))
            {
                bestCandidate = candidate;
                bestKey = candidateKey;
                bestTieBreakKey = candidateTieBreakKey;
                hasBestTieBreakKey = true;
            }
        }

        return bestCandidate;
    }

    public static AsOfJoinIndex<T, TKey> CreateAsOfIndex<T, TKey>(
        IEnumerable<T> candidates,
        Func<T, object?>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
    {
        return AsOfJoinIndex<T, TKey>.Create(candidates, equalityKeySelector, keySelector, comparisonKind);
    }

    public static AsOfJoinIndex<T, TKey> CreateAsOfIndex<T, TKey>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, object?>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
    {
        return AsOfJoinIndex<T, TKey>.CreateFromChunks(candidateChunks, equalityKeySelector, keySelector, comparisonKind);
    }

    public static AsOfJoinIndex<T, TKey> CreateAsOfIndex<T, TKey, TTie>(
        IEnumerable<T> candidates,
        Func<T, object?>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        Func<T, TTie> tieBreakKeySelector,
        bool tieBreakDescending,
        NullOrdering tieBreakNullOrdering)
        where T : class
    {
        return AsOfJoinIndex<T, TKey>.Create(
            candidates,
            equalityKeySelector,
            keySelector,
            comparisonKind,
            tieBreakKeySelector,
            tieBreakDescending,
            tieBreakNullOrdering);
    }

    public static AsOfJoinIndex<T, TKey> CreateAsOfIndex<T, TKey, TTie>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, object?>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        Func<T, TTie> tieBreakKeySelector,
        bool tieBreakDescending,
        NullOrdering tieBreakNullOrdering)
        where T : class
    {
        return AsOfJoinIndex<T, TKey>.CreateFromChunks(
            candidateChunks,
            equalityKeySelector,
            keySelector,
            comparisonKind,
            tieBreakKeySelector,
            tieBreakDescending,
            tieBreakNullOrdering);
    }

    public static RangeJoinIndex<T, TKey> CreateRangeJoinIndex<T, TKey>(
        IEnumerable<T> candidates,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
    {
        return RangeJoinIndex<T, TKey>.Create(candidates, keySelector, comparisonKind);
    }

    public static RangeJoinIndex<T, TKey> CreateRangeJoinIndex<T, TKey>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
    {
        return RangeJoinIndex<T, TKey>.CreateFromChunks(candidateChunks, keySelector, comparisonKind);
    }

    public static object? CreateAsOfEqualityKey(params object?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Length == 0)
            return null;

        foreach (var part in parts)
        {
            if (part is null)
                return null;
        }

        return WindowFunctionHelpers.CompositeKey(parts);
    }

    private static bool MatchesAsOfComparison(object probeValue, object candidateKey, BinaryOpKind comparisonKind)
    {
        var comparison = CompareAsOfValues(probeValue, candidateKey);

        return comparisonKind switch
        {
            BinaryOpKind.GreaterThan => comparison > 0,
            BinaryOpKind.GreaterOrEqual => comparison >= 0,
            BinaryOpKind.LessThan => comparison < 0,
            BinaryOpKind.LessOrEqual => comparison <= 0,
            _ => throw new InvalidOperationException(
                $"Unsupported ASOF comparison kind '{comparisonKind}'.")
        };
    }

    private static bool IsBetterAsOfCandidate(object candidateKey, object currentBestKey, BinaryOpKind comparisonKind)
    {
        var comparison = CompareAsOfValues(candidateKey, currentBestKey);

        return comparisonKind switch
        {
            BinaryOpKind.GreaterThan or BinaryOpKind.GreaterOrEqual => comparison > 0,
            BinaryOpKind.LessThan or BinaryOpKind.LessOrEqual => comparison < 0,
            _ => throw new InvalidOperationException(
                $"Unsupported ASOF comparison kind '{comparisonKind}'.")
        };
    }

    internal static int CompareAsOfValues(object left, object right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left is not IComparable comparable)
        {
            throw new InvalidOperationException(
                $"ASOF JOIN comparison requires an orderable left value, but encountered '{left.GetType().FullName}'.");
        }

        try
        {
            return comparable.CompareTo(right);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"ASOF JOIN comparison requires compatible values, but encountered '{left.GetType().FullName}' and '{right.GetType().FullName}'.",
                ex);
        }
    }

}
