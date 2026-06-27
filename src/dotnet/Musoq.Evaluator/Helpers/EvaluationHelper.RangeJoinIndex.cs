using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Helpers;

public sealed class RangeJoinIndex<TRow, TKey>
    where TRow : class
{
    private readonly RangeJoinEntry[] _entries;
    private readonly BinaryOpKind _comparisonKind;

    private RangeJoinIndex(
        RangeJoinEntry[] entries,
        BinaryOpKind comparisonKind)
    {
        _entries = entries;
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

        entries.Sort(static (left, right) =>
        {
            var comparison = CompareKeys(left.Key, right.Key);
            return comparison != 0
                ? comparison
                : left.Ordinal.CompareTo(right.Ordinal);
        });

        return new RangeJoinIndex<TRow, TKey>(entries.ToArray(), comparisonKind);
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

        entries.Sort(static (left, right) =>
        {
            var comparison = CompareKeys(left.Key, right.Key);
            return comparison != 0
                ? comparison
                : left.Ordinal.CompareTo(right.Ordinal);
        });

        return new RangeJoinIndex<TRow, TKey>(entries.ToArray(), comparisonKind);
    }

    public RangeJoinMatch Find(TKey probeValue)
    {
        if (IsNullKey(probeValue) || _entries.Length == 0)
            return RangeJoinMatch.Empty;

        return _comparisonKind switch
        {
            BinaryOpKind.GreaterThan => CreateMatch(0, FindFirstGreaterOrEqual(probeValue)),
            BinaryOpKind.GreaterOrEqual => CreateMatch(0, FindFirstGreaterThan(probeValue)),
            BinaryOpKind.LessThan => CreateMatch(FindFirstGreaterThan(probeValue), _entries.Length),
            BinaryOpKind.LessOrEqual => CreateMatch(FindFirstGreaterOrEqual(probeValue), _entries.Length),
            _ => throw new InvalidOperationException(
                $"Unsupported range join comparison kind '{_comparisonKind}'.")
        };
    }

    private RangeJoinMatch CreateMatch(int startInclusive, int endExclusive)
    {
        if (startInclusive >= endExclusive)
            return RangeJoinMatch.Empty;

        return new RangeJoinMatch(_entries, startInclusive, endExclusive);
    }

    private int FindFirstGreaterOrEqual(TKey probeValue)
    {
        var low = 0;
        var high = _entries.Length;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var comparison = CompareKeys(_entries[middle].Key, probeValue);

            if (comparison < 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    private int FindFirstGreaterThan(TKey probeValue)
    {
        var low = 0;
        var high = _entries.Length;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var comparison = CompareKeys(_entries[middle].Key, probeValue);

            if (comparison <= 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
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
