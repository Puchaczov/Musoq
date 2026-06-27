using System.Buffers;

namespace Musoq.Evaluator.Helpers;

public sealed class WindowIntOrderBuilder(int rowCount)
{
    private readonly int[] _indices = new int[rowCount];
    private readonly int[] _orderKeys = new int[rowCount];
    private int _addedCount;
    private WindowPartitionSet? _sortedPartitions;
    private bool _hasSortedPartitions;
    private bool _sortedDescending;

    public void Add(int orderKey, int rowIndex)
    {
        _orderKeys[rowIndex] = orderKey;
        _indices[_addedCount++] = rowIndex;
    }

    public WindowPartitionSet ToSortedPartitionSet(bool descending)
    {
        EnsureComplete();

        if (_hasSortedPartitions && _sortedDescending == descending && _sortedPartitions != null)
            return _sortedPartitions;

        var indices = (int[])_indices.Clone();
        var partitions = new WindowPartitionSet(rowCount, indices, [0], [rowCount]);
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

        for (var index = 0; index < indices.Length; index++)
            result[indices[index]] = index + 1L;

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
        var count = Math.Min(indices.Length, maxRowNumber);
        for (var index = 0; index < count; index++)
            result[indices[index]] = index + 1L;

        return result;
    }

    public long[] ComputeRank(bool descending)
    {
        return ComputeRank(ToSortedPartitionSet(descending));
    }

    public long[] ComputeRank(WindowPartitionSet sortedPartitions)
    {
        ArgumentNullException.ThrowIfNull(sortedPartitions);
        var result = new long[rowCount];
        var indices = sortedPartitions.Indices;
        long rank = 1;

        for (var index = 0; index < indices.Length; index++)
        {
            var currentIndex = indices[index];
            if (index > 0 && _orderKeys[currentIndex] != _orderKeys[indices[index - 1]])
                rank = index + 1L;

            result[currentIndex] = rank;
        }

        return result;
    }

    public long[] ComputeRankTopN(bool descending, long maxRank)
    {
        return ComputeRankTopN(ToSortedPartitionSet(descending), maxRank);
    }

    public long[] ComputeRankTopN(WindowPartitionSet sortedPartitions, long maxRank)
    {
        ArgumentNullException.ThrowIfNull(sortedPartitions);
        var result = new long[rowCount];
        if (maxRank < 1)
            return result;

        var indices = sortedPartitions.Indices;
        long rank = 1;

        for (var index = 0; index < indices.Length; index++)
        {
            var currentIndex = indices[index];
            if (index > 0 && _orderKeys[currentIndex] != _orderKeys[indices[index - 1]])
                rank = index + 1L;

            if (rank > maxRank)
                break;

            result[currentIndex] = rank;
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
        var result = new long[rowCount];
        var indices = sortedPartitions.Indices;
        long denseRank = 1;

        for (var index = 0; index < indices.Length; index++)
        {
            var currentIndex = indices[index];
            if (index > 0 && _orderKeys[currentIndex] != _orderKeys[indices[index - 1]])
                denseRank++;

            result[currentIndex] = denseRank;
        }

        return result;
    }

    public long[] ComputeDenseRankTopN(bool descending, long maxRank)
    {
        return ComputeDenseRankTopN(ToSortedPartitionSet(descending), maxRank);
    }

    public long[] ComputeDenseRankTopN(WindowPartitionSet sortedPartitions, long maxRank)
    {
        ArgumentNullException.ThrowIfNull(sortedPartitions);
        var result = new long[rowCount];
        if (maxRank < 1)
            return result;

        var indices = sortedPartitions.Indices;
        long denseRank = 1;

        for (var index = 0; index < indices.Length; index++)
        {
            var currentIndex = indices[index];
            if (index > 0 && _orderKeys[currentIndex] != _orderKeys[indices[index - 1]])
                denseRank++;

            if (denseRank > maxRank)
                break;

            result[currentIndex] = denseRank;
        }

        return result;
    }

    private void Sort(WindowPartitionSet partitions, bool descending)
    {
        var indices = partitions.Indices;
        var sortKeys = ArrayPool<uint>.Shared.Rent(indices.Length);
        try
        {
            for (var index = 0; index < indices.Length; index++)
            {
                var sortKey = (uint)(_orderKeys[indices[index]] ^ int.MinValue);
                sortKeys[index] = descending ? ~sortKey : sortKey;
            }

            Array.Sort(sortKeys, indices, 0, indices.Length);
        }
        finally
        {
            ArrayPool<uint>.Shared.Return(sortKeys);
        }
    }

    private void EnsureComplete()
    {
        if (_addedCount != rowCount)
            throw new InvalidOperationException("Window int order builder must receive exactly rowCount rows.");
    }
}
