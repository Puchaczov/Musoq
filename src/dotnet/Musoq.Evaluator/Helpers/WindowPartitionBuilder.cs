using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Musoq.Evaluator.Helpers;

#pragma warning disable CS8714
public sealed class WindowPartitionBuilder<T>(int rowCount, bool nullPartitionFirst = true)
{
    private const int NullPartition = -1;

    private readonly Dictionary<T, int> _keyToPartition = new(CreateDictionaryCapacity(rowCount));
    private readonly int[] _rowPartitions = new int[rowCount];
    private int[] _lengths = new int[4];
    private int _partitionCount;
    private int _nullLength;
    private int _addedCount;

    public void Add(T? key, int rowIndex)
    {
        _addedCount++;

        if (key == null)
        {
            _rowPartitions[rowIndex] = NullPartition;
            _nullLength++;
            return;
        }

        ref var partitionIndex = ref CollectionsMarshal.GetValueRefOrAddDefault(_keyToPartition, key, out var exists);

        if (!exists)
        {
            partitionIndex = _partitionCount;
            EnsurePartitionCapacity(_partitionCount + 1);
            _partitionCount++;
        }

        _rowPartitions[rowIndex] = partitionIndex;
        _lengths[partitionIndex]++;
    }

    public WindowPartitionSet ToPartitionSet()
    {
        if (_addedCount != rowCount)
            throw new InvalidOperationException("Window partition builder must receive exactly rowCount rows.");

        if (rowCount == 0)
            return WindowPartitionSet.Empty(rowCount);

        var hasNullPartition = _nullLength > 0;
        var totalPartitions = _partitionCount + (hasNullPartition ? 1 : 0);
        var starts = new int[totalPartitions];
        var lengths = new int[totalPartitions];
        var indices = new int[rowCount];
        var targetPartition = 0;

        if (hasNullPartition && nullPartitionFirst)
        {
            lengths[targetPartition] = _nullLength;
            targetPartition++;
        }

        for (var partitionIndex = 0; partitionIndex < _partitionCount; partitionIndex++)
        {
            lengths[targetPartition] = _lengths[partitionIndex];
            targetPartition++;
        }

        if (hasNullPartition && !nullPartitionFirst)
            lengths[targetPartition] = _nullLength;

        for (var partitionIndex = 1; partitionIndex < starts.Length; partitionIndex++)
            starts[partitionIndex] = starts[partitionIndex - 1] + lengths[partitionIndex - 1];

        FillPartitions(indices, starts, hasNullPartition && nullPartitionFirst);
        return new WindowPartitionSet(rowCount, indices, starts, lengths);
    }

    private void EnsurePartitionCapacity(int capacity)
    {
        if (capacity <= _lengths.Length)
            return;

        var newSize = Math.Max(capacity, _lengths.Length * 2);
        Array.Resize(ref _lengths, newSize);
    }

    private void FillPartitions(int[] indices, int[] starts, bool nullPartitionFirst)
    {
        var positions = (int[])starts.Clone();
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

    private static int CreateDictionaryCapacity(int rowCount)
    {
        return Math.Max(1, Math.Min(rowCount, 128));
    }
}
#pragma warning restore CS8714
