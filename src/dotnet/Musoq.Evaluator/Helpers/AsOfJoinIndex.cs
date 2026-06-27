using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Helpers;

public sealed partial class AsOfJoinIndex<T, TKey>
    where T : class
{
    private readonly BinaryOpKind _comparisonKind;
    private readonly IComparer<TKey> _comparer;
    private readonly IAsOfJoinBucket? _singleBucket;
    private readonly Dictionary<object, IAsOfJoinBucket>? _buckets;

    private AsOfJoinIndex(
        BinaryOpKind comparisonKind,
        IComparer<TKey> comparer,
        IAsOfJoinBucket? singleBucket,
        Dictionary<object, IAsOfJoinBucket>? buckets)
    {
        _comparisonKind = comparisonKind;
        _comparer = comparer;
        _singleBucket = singleBucket;
        _buckets = buckets;
    }

    public static AsOfJoinIndex<T, TKey> Create(
        IEnumerable<T> candidates,
        Func<T, object?>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(keySelector);
        ValidateComparisonKind(comparisonKind);

        var comparer = CreateComparer();

        return equalityKeySelector == null
            ? CreateSingleBucket(candidates, keySelector, comparisonKind, comparer)
            : CreatePartitionedBuckets(candidates, equalityKeySelector, keySelector, comparisonKind, comparer);
    }

    public static AsOfJoinIndex<T, TKey> CreateFromChunks(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, object?>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind)
    {
        ArgumentNullException.ThrowIfNull(candidateChunks);
        ArgumentNullException.ThrowIfNull(keySelector);
        ValidateComparisonKind(comparisonKind);

        var comparer = CreateComparer();

        return equalityKeySelector == null
            ? CreateSingleBucketFromChunks(candidateChunks, keySelector, comparisonKind, comparer)
            : CreatePartitionedBucketsFromChunks(candidateChunks, equalityKeySelector, keySelector, comparisonKind, comparer);
    }

    public static AsOfJoinIndex<T, TKey> Create<TTie>(
        IEnumerable<T> candidates,
        Func<T, object?>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        Func<T, TTie> tieBreakKeySelector,
        bool tieBreakDescending,
        NullOrdering tieBreakNullOrdering)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(tieBreakKeySelector);
        ValidateComparisonKind(comparisonKind);

        var comparer = CreateComparer();
        var tieBreakComparer = AsOfTieBreakComparerFactory.Create<TTie>(tieBreakDescending, tieBreakNullOrdering);

        return equalityKeySelector == null
            ? CreateSingleBucket(
                candidates,
                keySelector,
                comparisonKind,
                comparer,
                tieBreakKeySelector,
                tieBreakComparer)
            : CreatePartitionedBuckets(
                candidates,
                equalityKeySelector,
                keySelector,
                comparisonKind,
                comparer,
                tieBreakKeySelector,
                tieBreakComparer);
    }

    public static AsOfJoinIndex<T, TKey> CreateFromChunks<TTie>(
        IEnumerable<IReadOnlyList<T>> candidateChunks,
        Func<T, object?>? equalityKeySelector,
        Func<T, TKey> keySelector,
        BinaryOpKind comparisonKind,
        Func<T, TTie> tieBreakKeySelector,
        bool tieBreakDescending,
        NullOrdering tieBreakNullOrdering)
    {
        ArgumentNullException.ThrowIfNull(candidateChunks);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(tieBreakKeySelector);
        ValidateComparisonKind(comparisonKind);

        var comparer = CreateComparer();
        var tieBreakComparer = AsOfTieBreakComparerFactory.Create<TTie>(tieBreakDescending, tieBreakNullOrdering);

        return equalityKeySelector == null
            ? CreateSingleBucketFromChunks(
                candidateChunks,
                keySelector,
                comparisonKind,
                comparer,
                tieBreakKeySelector,
                tieBreakComparer)
            : CreatePartitionedBucketsFromChunks(
                candidateChunks,
                equalityKeySelector,
                keySelector,
                comparisonKind,
                comparer,
                tieBreakKeySelector,
                tieBreakComparer);
    }

    public T? Find(object? equalityKey, TKey probeValue)
    {
        if (probeValue is null)
            return null;

        if (_buckets != null)
        {
            if (equalityKey is null || !_buckets.TryGetValue(equalityKey, out var bucket))
                return null;

            return bucket.Find(probeValue, _comparisonKind, _comparer);
        }

        return _singleBucket?.Find(probeValue, _comparisonKind, _comparer);
    }

    private static IComparer<TKey> CreateComparer()
    {
        return typeof(TKey) == typeof(object)
            ? (IComparer<TKey>)(object)AsOfObjectKeyComparer.Instance
            : Comparer<TKey>.Default;
    }

    private static void ValidateComparisonKind(BinaryOpKind comparisonKind)
    {
        if (comparisonKind is not (BinaryOpKind.GreaterThan
            or BinaryOpKind.GreaterOrEqual
            or BinaryOpKind.LessThan
            or BinaryOpKind.LessOrEqual))
        {
            throw new InvalidOperationException(
                $"Unsupported ASOF comparison kind '{comparisonKind}'.");
        }
    }
}
