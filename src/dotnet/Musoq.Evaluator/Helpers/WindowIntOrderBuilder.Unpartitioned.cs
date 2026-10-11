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

    public int[] OrderKeys => _orderKeys;

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
