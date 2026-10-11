using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static AsOfJoinIndex<T, TKey, TEquality> CreateAsOfIndex<T, TKey, TEquality>(
        IEnumerable<T> candidates,
        Func<T, NativeEqualityKey<TEquality>>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
        where TEquality : notnull
    {
        return AsOfJoinIndex<T, TKey, TEquality>.Create(candidates, equalityKeySelector, keySelector, comparisonKind);
    }

    public static AsOfJoinIndex<T, TKey, TEquality> CreateAsOfIndex<T, TKey, TEquality>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, NativeEqualityKey<TEquality>>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
        where TEquality : notnull
    {
        return AsOfJoinIndex<T, TKey, TEquality>.CreateFromChunks(candidateChunks, equalityKeySelector, keySelector, comparisonKind);
    }

    public static AsOfJoinIndex<T, TKey, TEquality> CreateAsOfIndex<T, TKey, TEquality, TTie>(
        IEnumerable<T> candidates,
        Func<T, NativeEqualityKey<TEquality>>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        Func<T, TTie> tieBreakKeySelector,
        bool tieBreakDescending,
        NullOrdering tieBreakNullOrdering)
        where T : class
        where TEquality : notnull
    {
        return AsOfJoinIndex<T, TKey, TEquality>.Create(
            candidates,
            equalityKeySelector,
            keySelector,
            comparisonKind,
            tieBreakKeySelector,
            tieBreakDescending,
            tieBreakNullOrdering);
    }

    public static AsOfJoinIndex<T, TKey, TEquality> CreateAsOfIndex<T, TKey, TEquality, TTie>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, NativeEqualityKey<TEquality>>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        Func<T, TTie> tieBreakKeySelector,
        bool tieBreakDescending,
        NullOrdering tieBreakNullOrdering)
        where T : class
        where TEquality : notnull
    {
        return AsOfJoinIndex<T, TKey, TEquality>.CreateFromChunks(
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
        IEnumerable<T> candidates,
        Func<T, object?> partitionKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
    {
        return RangeJoinIndex<T, TKey>.Create<object>(
            candidates,
            candidate => partitionKeySelector(candidate)!,
            keySelector,
            comparisonKind);
    }

    public static RangeJoinIndex<T, TKey> CreateRangeJoinIndex<T, TPartitionKey, TKey>(
        IEnumerable<T> candidates,
        Func<T, TPartitionKey> partitionKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
    {
        return RangeJoinIndex<T, TKey>.Create(candidates, partitionKeySelector, keySelector, comparisonKind);
    }

    public static RangeJoinIndex<T, TKey> CreateRangeJoinIndex<T, TKey>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
    {
        return RangeJoinIndex<T, TKey>.CreateFromChunks(candidateChunks, keySelector, comparisonKind);
    }

    public static RangeJoinIndex<T, TKey> CreateRangeJoinIndex<T, TKey>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, object?> partitionKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
    {
        return RangeJoinIndex<T, TKey>.CreateFromChunks<object>(
            candidateChunks,
            candidate => partitionKeySelector(candidate)!,
            keySelector,
            comparisonKind);
    }

    public static RangeJoinIndex<T, TKey> CreateRangeJoinIndex<T, TPartitionKey, TKey>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, TPartitionKey> partitionKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
        where T : class
    {
        return RangeJoinIndex<T, TKey>.CreateFromChunks(candidateChunks, partitionKeySelector, keySelector, comparisonKind);
    }

}
