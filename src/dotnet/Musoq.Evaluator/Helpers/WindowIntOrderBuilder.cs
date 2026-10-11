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

    public int[] OrderKeys => _orderKeys;

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
