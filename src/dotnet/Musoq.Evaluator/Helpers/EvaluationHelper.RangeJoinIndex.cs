using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

#pragma warning disable CS8714 // Null partition keys are rejected before Dictionary access.

namespace Musoq.Evaluator.Helpers;

public sealed class RangeJoinIndex<TRow, TKey>
    where TRow : class
{
    private readonly RangeJoinEntry[] _entries;
    private readonly IPartitionLookup? _partitionedEntries;
    private readonly BinaryOpKind _comparisonKind;

    private RangeJoinIndex(
        RangeJoinEntry[] entries,
        IPartitionLookup? partitionedEntries,
        BinaryOpKind comparisonKind)
    {
        _entries = entries;
        _partitionedEntries = partitionedEntries;
        _comparisonKind = comparisonKind;
    }

    public static RangeJoinIndex<TRow, TKey> Create(
        IEnumerable<TRow> candidates,
        Func<TRow, TKey> keySelector,
        BinaryOpKind comparisonKind)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(keySelector);
        ValidateComparisonKind(comparisonKind);

        var entries = new List<RangeJoinEntry>();
        var ordinal = 0;

        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;

            var key = keySelector(candidate);
            if (IsNullKey(key))
                continue;

            entries.Add(new RangeJoinEntry(key, candidate, ordinal));
            ordinal++;
        }

        SortEntries(entries);

        return new RangeJoinIndex<TRow, TKey>(entries.ToArray(), null, comparisonKind);
    }

    public static RangeJoinIndex<TRow, TKey> Create<TPartitionKey>(
        IEnumerable<TRow> candidates,
        Func<TRow, TPartitionKey> partitionKeySelector,
        Func<TRow, TKey> keySelector,
        BinaryOpKind comparisonKind)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(partitionKeySelector);
        ArgumentNullException.ThrowIfNull(keySelector);
        ValidateComparisonKind(comparisonKind);

        var entries = new Dictionary<TPartitionKey, List<RangeJoinEntry>>();
        var ordinal = 0;
        foreach (var candidate in candidates)
            AddPartitionedCandidate(entries, candidate, partitionKeySelector, keySelector, ref ordinal);

        return CreatePartitioned(entries, comparisonKind);
    }

    public static RangeJoinIndex<TRow, TKey> CreateFromChunks(
        IEnumerable<IReadOnlyList<TRow>> candidateChunks,
        Func<TRow, TKey> keySelector,
        BinaryOpKind comparisonKind)
    {
        ArgumentNullException.ThrowIfNull(candidateChunks);
        ArgumentNullException.ThrowIfNull(keySelector);
        ValidateComparisonKind(comparisonKind);

        var entries = new List<RangeJoinEntry>();
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
                if (IsNullKey(key))
                    continue;

                entries.Add(new RangeJoinEntry(key, candidate, ordinal));
                ordinal++;
            }
        }

        SortEntries(entries);

        return new RangeJoinIndex<TRow, TKey>(entries.ToArray(), null, comparisonKind);
    }

    public static RangeJoinIndex<TRow, TKey> CreateFromChunks<TPartitionKey>(
        IEnumerable<IReadOnlyList<TRow>> candidateChunks,
        Func<TRow, TPartitionKey> partitionKeySelector,
        Func<TRow, TKey> keySelector,
        BinaryOpKind comparisonKind)
    {
        ArgumentNullException.ThrowIfNull(candidateChunks);
        ArgumentNullException.ThrowIfNull(partitionKeySelector);
        ArgumentNullException.ThrowIfNull(keySelector);
        ValidateComparisonKind(comparisonKind);

        var entries = new Dictionary<TPartitionKey, List<RangeJoinEntry>>();
        var ordinal = 0;
        foreach (var chunk in candidateChunks)
        {
            if (chunk is null)
                continue;

            for (var index = 0; index < chunk.Count; index++)
                AddPartitionedCandidate(entries, chunk[index], partitionKeySelector, keySelector, ref ordinal);
        }

        return CreatePartitioned(entries, comparisonKind);
    }

    public RangeJoinMatch Find(TKey probeValue)
    {
        if (IsNullKey(probeValue) || _entries.Length == 0)
            return RangeJoinMatch.Empty;

        return Find(_entries, probeValue);
    }

    public RangeJoinMatch Find<TPartitionKey>(TPartitionKey partitionKey, TKey probeValue)
    {
        if (partitionKey is null || IsNullKey(probeValue) ||
            _partitionedEntries is not PartitionLookup<TPartitionKey> partitions ||
            !partitions.Entries.TryGetValue(partitionKey, out var entries))
        {
            return RangeJoinMatch.Empty;
        }

        return Find(entries, probeValue);
    }

    private RangeJoinMatch Find(RangeJoinEntry[] entries, TKey probeValue)
    {
        return _comparisonKind switch
        {
            BinaryOpKind.GreaterThan => CreateMatch(entries, 0, FindFirstGreaterOrEqual(entries, probeValue)),
            BinaryOpKind.GreaterOrEqual => CreateMatch(entries, 0, FindFirstGreaterThan(entries, probeValue)),
            BinaryOpKind.LessThan => CreateMatch(entries, FindFirstGreaterThan(entries, probeValue), entries.Length),
            BinaryOpKind.LessOrEqual => CreateMatch(entries, FindFirstGreaterOrEqual(entries, probeValue), entries.Length),
            _ => throw new InvalidOperationException(
                $"Unsupported range join comparison kind '{_comparisonKind}'.")
        };
    }

    private static RangeJoinMatch CreateMatch(RangeJoinEntry[] entries, int startInclusive, int endExclusive)
    {
        if (startInclusive >= endExclusive)
            return RangeJoinMatch.Empty;

        return new RangeJoinMatch(entries, startInclusive, endExclusive);
    }

    private static int FindFirstGreaterOrEqual(RangeJoinEntry[] entries, TKey probeValue)
    {
        var low = 0;
        var high = entries.Length;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var comparison = CompareKeys(entries[middle].Key, probeValue);

            if (comparison < 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    private static int FindFirstGreaterThan(RangeJoinEntry[] entries, TKey probeValue)
    {
        var low = 0;
        var high = entries.Length;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var comparison = CompareKeys(entries[middle].Key, probeValue);

            if (comparison <= 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    private static void AddPartitionedCandidate<TPartitionKey>(
        Dictionary<TPartitionKey, List<RangeJoinEntry>> entries,
        TRow? candidate,
        Func<TRow, TPartitionKey> partitionKeySelector,
        Func<TRow, TKey> keySelector,
        ref int ordinal)
    {
        if (candidate is null)
            return;

        var partitionKey = partitionKeySelector(candidate);
        var key = keySelector(candidate);
        if (partitionKey is null || IsNullKey(key))
            return;

        if (!entries.TryGetValue(partitionKey, out var partition))
        {
            partition = [];
            entries.Add(partitionKey, partition);
        }

        partition.Add(new RangeJoinEntry(key, candidate, ordinal));
        ordinal++;
    }

    private static RangeJoinIndex<TRow, TKey> CreatePartitioned<TPartitionKey>(
        Dictionary<TPartitionKey, List<RangeJoinEntry>> entries,
        BinaryOpKind comparisonKind)
    {
        var partitions = new Dictionary<TPartitionKey, RangeJoinEntry[]>(entries.Count);
        foreach (var pair in entries)
        {
            SortEntries(pair.Value);
            partitions.Add(pair.Key, pair.Value.ToArray());
        }

        return new RangeJoinIndex<TRow, TKey>([], new PartitionLookup<TPartitionKey>(partitions), comparisonKind);
    }

    private static void SortEntries(List<RangeJoinEntry> entries)
    {
        entries.Sort(static (left, right) =>
        {
            var comparison = CompareKeys(left.Key, right.Key);
            return comparison != 0
                ? comparison
                : left.Ordinal.CompareTo(right.Ordinal);
        });
    }

    private static void ValidateComparisonKind(BinaryOpKind comparisonKind)
    {
        if (comparisonKind is not (BinaryOpKind.GreaterThan
            or BinaryOpKind.GreaterOrEqual
            or BinaryOpKind.LessThan
            or BinaryOpKind.LessOrEqual))
        {
            throw new InvalidOperationException(
                $"Unsupported range join comparison kind '{comparisonKind}'.");
        }
    }

    private static bool IsNullKey(TKey key)
    {
        return key is null;
    }

    private static int CompareKeys(TKey left, TKey right)
    {
        try
        {
            return Comparer<TKey>.Default.Compare(left, right);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"Range join comparison requires compatible values of type '{typeof(TKey).FullName}'.",
                ex);
        }
    }

    internal readonly record struct RangeJoinEntry(
        TKey Key,
        TRow Row,
        int Ordinal);

    private interface IPartitionLookup
    {
    }

    private sealed class PartitionLookup<TPartitionKey>(Dictionary<TPartitionKey, RangeJoinEntry[]> entries)
        : IPartitionLookup
    {
        public Dictionary<TPartitionKey, RangeJoinEntry[]> Entries { get; } = entries;
    }

    public readonly struct RangeJoinMatch
    {
        internal static readonly RangeJoinMatch Empty = new([], 0, 0);

        private readonly RangeJoinEntry[] _entries;
        private readonly int _startInclusive;
        private readonly int _endExclusive;

        internal RangeJoinMatch(
            RangeJoinEntry[] entries,
            int startInclusive,
            int endExclusive)
        {
            _entries = entries;
            _startInclusive = startInclusive;
            _endExclusive = endExclusive;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_entries, _startInclusive, _endExclusive);
        }
    }

    public struct Enumerator
    {
        private readonly RangeJoinEntry[] _entries;
        private readonly int _endExclusive;
        private int _index;

        internal Enumerator(
            RangeJoinEntry[] entries,
            int startInclusive,
            int endExclusive)
        {
            _entries = entries;
            _endExclusive = endExclusive;
            _index = startInclusive - 1;
        }

        public TRow Current => _entries[_index].Row;

        public bool MoveNext()
        {
            var next = _index + 1;
            if (next >= _endExclusive)
                return false;

            _index = next;
            return true;
        }
    }
}

#pragma warning restore CS8714
