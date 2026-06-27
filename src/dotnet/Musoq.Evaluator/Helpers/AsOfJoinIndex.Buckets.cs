using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Helpers;

public sealed partial class AsOfJoinIndex<T, TKey>
    where T : class
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

            entries.Sort((left, right) =>
            {
                var comparison = comparer.Compare(left.Key, right.Key);
                return comparison != 0
                    ? comparison
                    : left.Ordinal.CompareTo(right.Ordinal);
            });

            return new AsOfJoinBucket(entries.ToArray());
        }

        public T? Find(TKey probeValue, BinaryOpKind comparisonKind, IComparer<TKey> comparer)
        {
            if (_entries.Length == 0)
                return null;

            var index = comparisonKind switch
            {
                BinaryOpKind.GreaterOrEqual => FindLastCandidateBeforeProbe(probeValue, comparer, includeEqual: true),
                BinaryOpKind.GreaterThan => FindLastCandidateBeforeProbe(probeValue, comparer, includeEqual: false),
                BinaryOpKind.LessOrEqual => FindFirstCandidateAfterProbe(probeValue, comparer, includeEqual: true),
                BinaryOpKind.LessThan => FindFirstCandidateAfterProbe(probeValue, comparer, includeEqual: false),
                _ => throw new InvalidOperationException(
                    $"Unsupported ASOF comparison kind '{comparisonKind}'.")
            };

            return index < 0 ? null : _entries[index].Row;
        }

        private int FindLastCandidateBeforeProbe(TKey probeValue, IComparer<TKey> comparer, bool includeEqual)
        {
            var low = 0;
            var high = _entries.Length - 1;
            var result = -1;

            while (low <= high)
            {
                var middle = low + ((high - low) / 2);
                var comparison = comparer.Compare(probeValue, _entries[middle].Key);
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

            return MoveToFirstEntryWithSameKey(result, comparer);
        }

        private int FindFirstCandidateAfterProbe(TKey probeValue, IComparer<TKey> comparer, bool includeEqual)
        {
            var low = 0;
            var high = _entries.Length - 1;
            var result = -1;

            while (low <= high)
            {
                var middle = low + ((high - low) / 2);
                var comparison = comparer.Compare(probeValue, _entries[middle].Key);
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

        private int MoveToFirstEntryWithSameKey(int index, IComparer<TKey> comparer)
        {
            if (index <= 0)
                return index;

            while (index > 0 &&
                   comparer.Compare(_entries[index].Key, _entries[index - 1].Key) == 0)
            {
                index--;
            }

            return index;
        }
    }

    private readonly record struct AsOfJoinEntry(
        TKey Key,
        T Row,
        int Ordinal);

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
            IComparer<TTie> tieBreakComparer)
        {
            if (entries.Count == 0)
                return Empty;

            entries.Sort((left, right) =>
            {
                var comparison = comparer.Compare(left.Key, right.Key);
                if (comparison != 0)
                    return comparison;

                comparison = tieBreakComparer.Compare(left.TieBreakKey, right.TieBreakKey);
                return comparison != 0
                    ? comparison
                    : left.Ordinal.CompareTo(right.Ordinal);
            });

            return new AsOfJoinTieBucket<TTie>(entries.ToArray());
        }

        public T? Find(TKey probeValue, BinaryOpKind comparisonKind, IComparer<TKey> comparer)
        {
            if (_entries.Length == 0)
                return null;

            var index = comparisonKind switch
            {
                BinaryOpKind.GreaterOrEqual => FindLastCandidateBeforeProbe(probeValue, comparer, includeEqual: true),
                BinaryOpKind.GreaterThan => FindLastCandidateBeforeProbe(probeValue, comparer, includeEqual: false),
                BinaryOpKind.LessOrEqual => FindFirstCandidateAfterProbe(probeValue, comparer, includeEqual: true),
                BinaryOpKind.LessThan => FindFirstCandidateAfterProbe(probeValue, comparer, includeEqual: false),
                _ => throw new InvalidOperationException(
                    $"Unsupported ASOF comparison kind '{comparisonKind}'.")
            };

            return index < 0 ? null : _entries[index].Row;
        }

        private int FindLastCandidateBeforeProbe(TKey probeValue, IComparer<TKey> comparer, bool includeEqual)
        {
            var low = 0;
            var high = _entries.Length - 1;
            var result = -1;

            while (low <= high)
            {
                var middle = low + ((high - low) / 2);
                var comparison = comparer.Compare(probeValue, _entries[middle].Key);
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

            return MoveToFirstEntryWithSameKey(result, comparer);
        }

        private int FindFirstCandidateAfterProbe(TKey probeValue, IComparer<TKey> comparer, bool includeEqual)
        {
            var low = 0;
            var high = _entries.Length - 1;
            var result = -1;

            while (low <= high)
            {
                var middle = low + ((high - low) / 2);
                var comparison = comparer.Compare(probeValue, _entries[middle].Key);
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

        private int MoveToFirstEntryWithSameKey(int index, IComparer<TKey> comparer)
        {
            if (index <= 0)
                return index;

            while (index > 0 &&
                   comparer.Compare(_entries[index].Key, _entries[index - 1].Key) == 0)
            {
                index--;
            }

            return index;
        }
    }

    private readonly record struct AsOfJoinTieEntry<TTie>(
        TKey Key,
        T Row,
        TTie TieBreakKey,
        int Ordinal);
}
