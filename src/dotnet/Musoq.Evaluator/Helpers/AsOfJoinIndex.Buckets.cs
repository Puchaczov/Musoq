using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Helpers;

public sealed partial class AsOfJoinIndex<T, TKey, TEquality>
    where T : class
    where TEquality : notnull
{
    private interface IAsOfJoinBucket
    {
        T? Find(TKey probeValue, BinaryOpKind comparisonKind, IComparer<TKey> comparer);
    }

    private sealed class AsOfJoinBucket : IAsOfJoinBucket
    {
        private static readonly AsOfJoinBucket Empty = new([]);
        private readonly AsOfJoinEntry[] _entries;

        private AsOfJoinBucket(AsOfJoinEntry[] entries)
        {
            _entries = entries;
        }

        public static AsOfJoinBucket Create(List<AsOfJoinEntry> entries, IComparer<TKey> comparer)
        {
            if (entries.Count == 0)
                return Empty;

            entries.Sort(
                (left, right) =>
                {
                    var comparison = comparer.Compare(left.Key, right.Key);
                    return comparison != 0 ? comparison : left.Ordinal.CompareTo(right.Ordinal);
                }
            );

            return new AsOfJoinBucket(entries.ToArray());
        }

        public T? Find(TKey probeValue, BinaryOpKind comparisonKind, IComparer<TKey> comparer)
        {
            if (_entries.Length == 0)
                return null;

            var index = comparisonKind switch
            {
                BinaryOpKind.GreaterOrEqual => FindLastCandidateBeforeProbe(
                    _entries,
                    probeValue,
                    comparer,
                    includeEqual: true
                ),
                BinaryOpKind.GreaterThan => FindLastCandidateBeforeProbe(
                    _entries,
                    probeValue,
                    comparer,
                    includeEqual: false
                ),
                BinaryOpKind.LessOrEqual => FindFirstCandidateAfterProbe(
                    _entries,
                    probeValue,
                    comparer,
                    includeEqual: true
                ),
                BinaryOpKind.LessThan => FindFirstCandidateAfterProbe(
                    _entries,
                    probeValue,
                    comparer,
                    includeEqual: false
                ),
                _ => throw new InvalidOperationException($"Unsupported ASOF comparison kind '{comparisonKind}'."),
            };

            return index < 0 ? null : _entries[index].Row;
        }
    }

    private readonly record struct AsOfJoinEntry(TKey Key, T Row, int Ordinal) : IOrderedEntry;

    private sealed class AsOfJoinTieBucket<TTie> : IAsOfJoinBucket
    {
        private static readonly AsOfJoinTieBucket<TTie> Empty = new([]);
        private readonly AsOfJoinTieEntry<TTie>[] _entries;

        private AsOfJoinTieBucket(AsOfJoinTieEntry<TTie>[] entries)
        {
            _entries = entries;
        }

        public static AsOfJoinTieBucket<TTie> Create(
            List<AsOfJoinTieEntry<TTie>> entries,
            IComparer<TKey> comparer,
            IComparer<TTie> tieBreakComparer
        )
        {
            if (entries.Count == 0)
                return Empty;

            entries.Sort(
                (left, right) =>
                {
                    var comparison = comparer.Compare(left.Key, right.Key);
                    if (comparison != 0)
                        return comparison;

                    comparison = tieBreakComparer.Compare(left.TieBreakKey, right.TieBreakKey);
                    return comparison != 0 ? comparison : left.Ordinal.CompareTo(right.Ordinal);
                }
            );

            return new AsOfJoinTieBucket<TTie>(entries.ToArray());
        }

        public T? Find(TKey probeValue, BinaryOpKind comparisonKind, IComparer<TKey> comparer)
        {
            if (_entries.Length == 0)
                return null;

            var index = comparisonKind switch
            {
                BinaryOpKind.GreaterOrEqual => FindLastCandidateBeforeProbe(
                    _entries,
                    probeValue,
                    comparer,
                    includeEqual: true
                ),
                BinaryOpKind.GreaterThan => FindLastCandidateBeforeProbe(
                    _entries,
                    probeValue,
                    comparer,
                    includeEqual: false
                ),
                BinaryOpKind.LessOrEqual => FindFirstCandidateAfterProbe(
                    _entries,
                    probeValue,
                    comparer,
                    includeEqual: true
                ),
                BinaryOpKind.LessThan => FindFirstCandidateAfterProbe(
                    _entries,
                    probeValue,
                    comparer,
                    includeEqual: false
                ),
                _ => throw new InvalidOperationException($"Unsupported ASOF comparison kind '{comparisonKind}'."),
            };

            return index < 0 ? null : _entries[index].Row;
        }
    }

    private readonly record struct AsOfJoinTieEntry<TTie>(TKey Key, T Row, TTie TieBreakKey, int Ordinal)
        : IOrderedEntry;

    private static int FindLastCandidateBeforeProbe<TEntry>(
        TEntry[] entries,
        TKey probeValue,
        IComparer<TKey> comparer,
        bool includeEqual
    )
        where TEntry : struct, IOrderedEntry
    {
        var low = 0;
        var high = entries.Length - 1;
        var result = -1;

        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var comparison = comparer.Compare(probeValue, entries[middle].Key);
            var matches = includeEqual ? comparison >= 0 : comparison > 0;

            if (matches)
            {
                result = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return FirstEqual(entries, result, comparer);
    }

    private static int FindFirstCandidateAfterProbe<TEntry>(
        TEntry[] entries,
        TKey probeValue,
        IComparer<TKey> comparer,
        bool includeEqual
    )
        where TEntry : struct, IOrderedEntry
    {
        var low = 0;
        var high = entries.Length - 1;
        var result = -1;

        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var comparison = comparer.Compare(probeValue, entries[middle].Key);
            var matches = includeEqual ? comparison <= 0 : comparison < 0;

            if (matches)
            {
                result = middle;
                high = middle - 1;
            }
            else
            {
                low = middle + 1;
            }
        }

        return result;
    }

    private interface IOrderedEntry
    {
        TKey Key { get; }
    }

    private static int FirstEqual<TEntry>(TEntry[] entries, int index, IComparer<TKey> comparer)
        where TEntry : struct, IOrderedEntry
    {
        if (index <= 0)
            return index;

        var key = entries[index].Key;
        if (comparer.Compare(key, entries[index - 1].Key) != 0)
            return index;

        var low = 0;
        var high = index - 1;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (comparer.Compare(entries[middle].Key, key) < 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
}
