using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Musoq.Evaluator.Helpers;

#pragma warning disable CS8714
public sealed class WindowIntOrderBuilder<TPartition>(int rowCount, bool nullPartitionFirst = true)
{
    private const int NullPartition = -1;

    private readonly Dictionary<TPartition, int> _keyToPartition = new(CreateDictionaryCapacity(rowCount));
    private readonly int[] _rowPartitions = new int[rowCount];
    private readonly int[] _orderKeys = new int[rowCount];
    private int[] _lengths = new int[CreateDictionaryCapacity(rowCount)];
    private int _partitionCount;
    private int _nullLength;
    private int _addedCount;
    private WindowPartitionSet? _sortedPartitions;
    private bool _hasSortedPartitions;
    private bool _sortedDescending;

    public void Add(TPartition? partitionKey, int orderKey, int rowIndex)
    {
        _addedCount++;
        _orderKeys[rowIndex] = orderKey;

        if (partitionKey == null)
        {
            _rowPartitions[rowIndex] = NullPartition;
            _nullLength++;
            return;
        }

        ref var partitionIndex = ref CollectionsMarshal.GetValueRefOrAddDefault(_keyToPartition, partitionKey, out var exists);

        if (!exists)
        {
            partitionIndex = _partitionCount;
            EnsurePartitionCapacity(_partitionCount + 1);
            _partitionCount++;
        }

        _rowPartitions[rowIndex] = partitionIndex;
        _lengths[partitionIndex]++;
    }

    public WindowPartitionSet ToSortedPartitionSet(bool descending)
    {
        EnsureComplete();

        if (_hasSortedPartitions && _sortedDescending == descending && _sortedPartitions != null)
            return _sortedPartitions;

        var partitions = BuildPartitionSet();
        Sort(partitions, descending);
        _sortedPartitions = partitions;
        _sortedDescending = descending;
        _hasSortedPartitions = true;
        return partitions;
    }

    public long[] ComputeRowNumber(bool descending)
    {
        return ComputeRowNumber(ToSortedPartitionSet(descending));
    }

    public long[] ComputeRowNumber(WindowPartitionSet sortedPartitions)
    {
        ArgumentNullException.ThrowIfNull(sortedPartitions);
        var result = new long[rowCount];
        var indices = sortedPartitions.Indices;

        for (var partitionIndex = 0; partitionIndex < sortedPartitions.PartitionCount; partitionIndex++)
        {
            var start = sortedPartitions.GetStart(partitionIndex);
            var count = sortedPartitions.GetLength(partitionIndex);
            for (var index = 0; index < count; index++)
                result[indices[start + index]] = index + 1L;
        }

        return result;
    }

    public long[] ComputeRowNumberTopN(bool descending, long maxRowNumber)
    {
        return ComputeRowNumberTopN(ToSortedPartitionSet(descending), maxRowNumber);
    }

    public long[] ComputeRowNumberTopN(WindowPartitionSet sortedPartitions, long maxRowNumber)
    {
        ArgumentNullException.ThrowIfNull(sortedPartitions);
        var result = new long[rowCount];
        if (maxRowNumber < 1)
            return result;

        var indices = sortedPartitions.Indices;

        for (var partitionIndex = 0; partitionIndex < sortedPartitions.PartitionCount; partitionIndex++)
        {
            var start = sortedPartitions.GetStart(partitionIndex);
            var count = Math.Min(sortedPartitions.GetLength(partitionIndex), maxRowNumber);
            for (var index = 0; index < count; index++)
                result[indices[start + index]] = index + 1L;
        }

        return result;
    }

    public long[] ComputeRank(bool descending)
    {
        return ComputeRank(ToSortedPartitionSet(descending));
    }

    public long[] ComputeRank(WindowPartitionSet sortedPartitions)
    {
        ArgumentNullException.ThrowIfNull(sortedPartitions);
        return ComputeRank(sortedPartitions, long.MaxValue);
    }

    public long[] ComputeRankTopN(bool descending, long maxRank)
    {
        return ComputeRankTopN(ToSortedPartitionSet(descending), maxRank);
    }

    public long[] ComputeRankTopN(WindowPartitionSet sortedPartitions, long maxRank)
    {
        ArgumentNullException.ThrowIfNull(sortedPartitions);
        return ComputeRank(sortedPartitions, maxRank);
    }

    private long[] ComputeRank(WindowPartitionSet sortedPartitions, long maxRank)
    {
        var result = new long[rowCount];
        if (maxRank < 1)
            return result;

        var indices = sortedPartitions.Indices;

        for (var partitionIndex = 0; partitionIndex < sortedPartitions.PartitionCount; partitionIndex++)
        {
            long rank = 1;
            var start = sortedPartitions.GetStart(partitionIndex);
            var count = sortedPartitions.GetLength(partitionIndex);
            for (var index = 0; index < count; index++)
            {
                var currentIndex = indices[start + index];
                if (HasOrderKeyChanged(indices, start, index))
                    rank = index + 1L;

                if (rank > maxRank)
                    break;

                result[currentIndex] = rank;
            }
        }

        return result;
    }

    public long[] ComputeDenseRank(bool descending)
    {
        return ComputeDenseRank(ToSortedPartitionSet(descending));
    }

    public long[] ComputeDenseRank(WindowPartitionSet sortedPartitions)
    {
        ArgumentNullException.ThrowIfNull(sortedPartitions);
        return ComputeDenseRank(sortedPartitions, long.MaxValue);
    }

    public long[] ComputeDenseRankTopN(bool descending, long maxRank)
    {
        return ComputeDenseRankTopN(ToSortedPartitionSet(descending), maxRank);
    }

    public long[] ComputeDenseRankTopN(WindowPartitionSet sortedPartitions, long maxRank)
    {
        ArgumentNullException.ThrowIfNull(sortedPartitions);
        return ComputeDenseRank(sortedPartitions, maxRank);
    }

    private long[] ComputeDenseRank(WindowPartitionSet sortedPartitions, long maxRank)
    {
        var result = new long[rowCount];
        if (maxRank < 1)
            return result;

        var indices = sortedPartitions.Indices;

        for (var partitionIndex = 0; partitionIndex < sortedPartitions.PartitionCount; partitionIndex++)
        {
            long denseRank = 1;
            var start = sortedPartitions.GetStart(partitionIndex);
            var count = sortedPartitions.GetLength(partitionIndex);
            for (var index = 0; index < count; index++)
            {
                var currentIndex = indices[start + index];
                if (HasOrderKeyChanged(indices, start, index))
                    denseRank++;

                if (denseRank > maxRank)
                    break;

                result[currentIndex] = denseRank;
            }
        }

        return result;
    }

    private bool HasOrderKeyChanged(int[] indices, int start, int index)
    {
        return index > 0 && _orderKeys[indices[start + index]] != _orderKeys[indices[start + index - 1]];
    }

    private WindowPartitionSet BuildPartitionSet()
    {
        if (rowCount == 0)
            return WindowPartitionSet.Empty(rowCount);

        var hasNullPartition = _nullLength > 0;
        var totalPartitions = _partitionCount + (hasNullPartition ? 1 : 0);
        var starts = new int[totalPartitions];
        var lengths = new int[totalPartitions];
        var indices = new int[rowCount];
        var targetPartition = 0;

        if (hasNullPartition && nullPartitionFirst)
            lengths[targetPartition++] = _nullLength;

        for (var partitionIndex = 0; partitionIndex < _partitionCount; partitionIndex++)
            lengths[targetPartition++] = _lengths[partitionIndex];

        if (hasNullPartition && !nullPartitionFirst)
            lengths[targetPartition] = _nullLength;

        for (var partitionIndex = 1; partitionIndex < starts.Length; partitionIndex++)
            starts[partitionIndex] = starts[partitionIndex - 1] + lengths[partitionIndex - 1];

        Span<int> positions = starts.Length <= 128
            ? stackalloc int[starts.Length]
            : new int[starts.Length];
        starts.CopyTo(positions);

        FillPartitions(indices, positions, hasNullPartition && nullPartitionFirst);
        return new WindowPartitionSet(rowCount, indices, starts, lengths);
    }

    private void FillPartitions(int[] indices, Span<int> positions, bool nullPartitionFirst)
    {
        var nonNullPartitionOffset = nullPartitionFirst ? 1 : 0;
        var nullPartitionIndex = nullPartitionFirst ? 0 : _partitionCount;

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var partitionIndex = _rowPartitions[rowIndex];
            var targetPartition = partitionIndex == NullPartition
                ? nullPartitionIndex
                : partitionIndex + nonNullPartitionOffset;
            indices[positions[targetPartition]++] = rowIndex;
        }
    }

    private void Sort(WindowPartitionSet partitions, bool descending)
    {
        var indices = partitions.Indices;
        var sortKeys = ArrayPool<uint>.Shared.Rent(indices.Length);
        try
        {
            for (var partitionIndex = 0; partitionIndex < partitions.PartitionCount; partitionIndex++)
            {
                var start = partitions.GetStart(partitionIndex);
                var count = partitions.GetLength(partitionIndex);
                if (count <= 1)
                    continue;

                for (var index = 0; index < count; index++)
                {
                    var sortKey = (uint)(_orderKeys[indices[start + index]] ^ int.MinValue);
                    sortKeys[start + index] = descending ? ~sortKey : sortKey;
                }

                Array.Sort(sortKeys, indices, start, count);
            }
        }
        finally
        {
            ArrayPool<uint>.Shared.Return(sortKeys);
        }
    }

    private void EnsurePartitionCapacity(int capacity)
    {
        if (capacity <= _lengths.Length)
            return;

        var newSize = Math.Max(capacity, _lengths.Length * 2);
        Array.Resize(ref _lengths, newSize);
    }

    private void EnsureComplete()
    {
        if (_addedCount != rowCount)
            throw new InvalidOperationException("Window int order builder must receive exactly rowCount rows.");
    }

    private static int CreateDictionaryCapacity(int rowCount)
    {
        return Math.Max(1, Math.Min(rowCount, 16));
    }
}
#pragma warning restore CS8714
