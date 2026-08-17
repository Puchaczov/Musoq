using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Helpers;

public sealed partial class AsOfJoinIndex<T, TKey>
    where T : class
{
    private static AsOfJoinIndex<T, TKey> CreateSingleBucket(
        IEnumerable<T> candidates,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        IComparer<TKey> comparer)
    {
        var entries = new List<AsOfJoinEntry>();
        var ordinal = 0;

        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;

            var key = keySelector(candidate);
            if (key is null)
                continue;

            entries.Add(new AsOfJoinEntry(key, candidate, ordinal));
            ordinal++;
        }

        return new AsOfJoinIndex<T, TKey>(
            comparisonKind,
            comparer,
            AsOfJoinBucket.Create(entries, comparer),
            null);
    }

    private static AsOfJoinIndex<T, TKey> CreateSingleBucketFromChunks(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        IComparer<TKey> comparer)
    {
        var entries = new List<AsOfJoinEntry>();
        var ordinal = 0;

        foreach (var chunk in candidateChunks)
        {
            if (chunk is null)
                continue;

            entries.EnsureCapacity(entries.Count + chunk.Count);
            for (var index = 0; index < chunk.Count; index++)
            {
                var candidate = chunk[index];
                if (candidate is null)
                    continue;

                var key = keySelector(candidate);
                if (key is null)
                    continue;

                entries.Add(new AsOfJoinEntry(key, candidate, ordinal));
                ordinal++;
            }
        }

        return new AsOfJoinIndex<T, TKey>(
            comparisonKind,
            comparer,
            AsOfJoinBucket.Create(entries, comparer),
            null);
    }

    private static AsOfJoinIndex<T, TKey> CreateSingleBucket<TTie>(
        IEnumerable<T> candidates,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        IComparer<TKey> comparer,
        Func<T, TTie> tieBreakKeySelector,
        IComparer<TTie> tieBreakComparer)
    {
        var entries = new List<AsOfJoinTieEntry<TTie>>();
        var ordinal = 0;

        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;

            var key = keySelector(candidate);
            if (key is null)
                continue;

            entries.Add(new AsOfJoinTieEntry<TTie>(key, candidate, tieBreakKeySelector(candidate), ordinal));
            ordinal++;
        }

        return new AsOfJoinIndex<T, TKey>(
            comparisonKind,
            comparer,
            AsOfJoinTieBucket<TTie>.Create(entries, comparer, tieBreakComparer),
            null);
    }

    private static AsOfJoinIndex<T, TKey> CreateSingleBucketFromChunks<TTie>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        IComparer<TKey> comparer,
        Func<T, TTie> tieBreakKeySelector,
        IComparer<TTie> tieBreakComparer)
    {
        var entries = new List<AsOfJoinTieEntry<TTie>>();
        var ordinal = 0;

        foreach (var chunk in candidateChunks)
        {
            if (chunk is null)
                continue;

            entries.EnsureCapacity(entries.Count + chunk.Count);
            for (var index = 0; index < chunk.Count; index++)
            {
                var candidate = chunk[index];
                if (candidate is null)
                    continue;

                var key = keySelector(candidate);
                if (key is null)
                    continue;

                entries.Add(new AsOfJoinTieEntry<TTie>(key, candidate, tieBreakKeySelector(candidate), ordinal));
                ordinal++;
            }
        }

        return new AsOfJoinIndex<T, TKey>(
            comparisonKind,
            comparer,
            AsOfJoinTieBucket<TTie>.Create(entries, comparer, tieBreakComparer),
            null);
    }

    private static AsOfJoinIndex<T, TKey> CreatePartitionedBuckets(
        IEnumerable<T> candidates,
        Func<T, object?> equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        IComparer<TKey> comparer)
    {
        var entriesByEqualityKey = new Dictionary<object, List<AsOfJoinEntry>>();
        var ordinal = 0;

        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;

            var equalityKey = equalityKeySelector(candidate);
            if (equalityKey is null)
                continue;

            var key = keySelector(candidate);
            if (key is null)
                continue;

            if (!entriesByEqualityKey.TryGetValue(equalityKey, out var entries))
            {
                entries = [];
                entriesByEqualityKey.Add(equalityKey, entries);
            }

            entries.Add(new AsOfJoinEntry(key, candidate, ordinal));
            ordinal++;
        }

        return new AsOfJoinIndex<T, TKey>(
            comparisonKind,
            comparer,
            null,
            entriesByEqualityKey.ToDictionary(
                static pair => pair.Key,
                pair => (IAsOfJoinBucket)AsOfJoinBucket.Create(pair.Value, comparer)));
    }

    private static AsOfJoinIndex<T, TKey> CreatePartitionedBucketsFromChunks(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, object?> equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        IComparer<TKey> comparer)
    {
        var entriesByEqualityKey = new Dictionary<object, List<AsOfJoinEntry>>();
        var ordinal = 0;

        foreach (var chunk in candidateChunks)
        {
            if (chunk is null)
                continue;

            for (var index = 0; index < chunk.Count; index++)
            {
                var candidate = chunk[index];
                if (candidate is null)
                    continue;

                var equalityKey = equalityKeySelector(candidate);
                if (equalityKey is null)
                    continue;

                var key = keySelector(candidate);
                if (key is null)
                    continue;

                if (!entriesByEqualityKey.TryGetValue(equalityKey, out var entries))
                {
                    entries = [];
                    entriesByEqualityKey.Add(equalityKey, entries);
                }

                entries.Add(new AsOfJoinEntry(key, candidate, ordinal));
                ordinal++;
            }
        }

        return new AsOfJoinIndex<T, TKey>(
            comparisonKind,
            comparer,
            null,
            entriesByEqualityKey.ToDictionary(
                static pair => pair.Key,
                pair => (IAsOfJoinBucket)AsOfJoinBucket.Create(pair.Value, comparer)));
    }

    private static AsOfJoinIndex<T, TKey> CreatePartitionedBuckets<TTie>(
        IEnumerable<T> candidates,
        Func<T, object?> equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        IComparer<TKey> comparer,
        Func<T, TTie> tieBreakKeySelector,
        IComparer<TTie> tieBreakComparer)
    {
        var entriesByEqualityKey = new Dictionary<object, List<AsOfJoinTieEntry<TTie>>>();
        var ordinal = 0;

        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;

            var equalityKey = equalityKeySelector(candidate);
            if (equalityKey is null)
                continue;

            var key = keySelector(candidate);
            if (key is null)
                continue;

            if (!entriesByEqualityKey.TryGetValue(equalityKey, out var entries))
            {
                entries = [];
                entriesByEqualityKey.Add(equalityKey, entries);
            }

            entries.Add(new AsOfJoinTieEntry<TTie>(key, candidate, tieBreakKeySelector(candidate), ordinal));
            ordinal++;
        }

        var buckets = new Dictionary<object, IAsOfJoinBucket>(entriesByEqualityKey.Count);
        foreach (var pair in entriesByEqualityKey)
            buckets.Add(pair.Key, AsOfJoinTieBucket<TTie>.Create(pair.Value, comparer, tieBreakComparer));

        return new AsOfJoinIndex<T, TKey>(
            comparisonKind,
            comparer,
            null,
            buckets);
    }

    private static AsOfJoinIndex<T, TKey> CreatePartitionedBucketsFromChunks<TTie>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, object?> equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        IComparer<TKey> comparer,
        Func<T, TTie> tieBreakKeySelector,
        IComparer<TTie> tieBreakComparer)
    {
        var entriesByEqualityKey = new Dictionary<object, List<AsOfJoinTieEntry<TTie>>>();
        var ordinal = 0;

        foreach (var chunk in candidateChunks)
        {
            if (chunk is null)
                continue;

            for (var index = 0; index < chunk.Count; index++)
            {
                var candidate = chunk[index];
                if (candidate is null)
                    continue;

                var equalityKey = equalityKeySelector(candidate);
                if (equalityKey is null)
                    continue;

                var key = keySelector(candidate);
                if (key is null)
                    continue;

                if (!entriesByEqualityKey.TryGetValue(equalityKey, out var entries))
                {
                    entries = [];
                    entriesByEqualityKey.Add(equalityKey, entries);
                }

                entries.Add(new AsOfJoinTieEntry<TTie>(key, candidate, tieBreakKeySelector(candidate), ordinal));
                ordinal++;
            }
        }

        var buckets = new Dictionary<object, IAsOfJoinBucket>(entriesByEqualityKey.Count);
        foreach (var pair in entriesByEqualityKey)
            buckets.Add(pair.Key, AsOfJoinTieBucket<TTie>.Create(pair.Value, comparer, tieBreakComparer));

        return new AsOfJoinIndex<T, TKey>(
            comparisonKind,
            comparer,
            null,
            buckets);
    }

}
