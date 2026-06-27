using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public sealed class WindowPartitionSet
{
    private readonly int[] _starts;
    private readonly int[] _lengths;

    public WindowPartitionSet(int rowCount, int[] indices, int[] starts, int[] lengths)
    {
        ArgumentNullException.ThrowIfNull(indices);
        ArgumentNullException.ThrowIfNull(starts);
        ArgumentNullException.ThrowIfNull(lengths);
        if (starts.Length != lengths.Length)
            throw new ArgumentException("Partition starts and lengths must have the same size.", nameof(lengths));

        RowCount = rowCount;
        Indices = indices;
        _starts = starts;
        _lengths = lengths;
    }

    public int RowCount { get; }

    public int[] Indices { get; }

    public int PartitionCount => _starts.Length;

    public int GetStart(int partitionIndex)
    {
        return _starts[partitionIndex];
    }

    public int GetLength(int partitionIndex)
    {
        return _lengths[partitionIndex];
    }

    public int GetIndex(int partitionIndex, int indexInPartition)
    {
        return Indices[_starts[partitionIndex] + indexInPartition];
    }

    public ReadOnlySpan<int> GetPartitionSpan(int partitionIndex)
    {
        return new ReadOnlySpan<int>(Indices, _starts[partitionIndex], _lengths[partitionIndex]);
    }

    public WindowPartitionSet Copy()
    {
        return new WindowPartitionSet(
            RowCount,
            (int[])Indices.Clone(),
            (int[])_starts.Clone(),
            (int[])_lengths.Clone());
    }

    public List<List<int>> ToLists()
    {
        var result = new List<List<int>>(PartitionCount);

        for (var partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
        {
            var length = _lengths[partitionIndex];
            var start = _starts[partitionIndex];
            var partition = new List<int>(length);

            for (var index = 0; index < length; index++)
                partition.Add(Indices[start + index]);

            result.Add(partition);
        }

        return result;
    }

    public static WindowPartitionSet Sequential(int rowCount)
    {
        var indices = new int[rowCount];
        for (var index = 0; index < rowCount; index++)
            indices[index] = index;

        return new WindowPartitionSet(rowCount, indices, [0], [rowCount]);
    }

    public static WindowPartitionSet Empty(int rowCount)
    {
        return new WindowPartitionSet(rowCount, [], [], []);
    }

    public static WindowPartitionSet FromLists(int rowCount, List<List<int>> partitions)
    {
        ArgumentNullException.ThrowIfNull(partitions);
        if (rowCount == 0)
            return Empty(rowCount);

        var starts = new int[partitions.Count];
        var lengths = new int[partitions.Count];
        var indices = new int[rowCount];
        var nextStart = 0;

        for (var partitionIndex = 0; partitionIndex < partitions.Count; partitionIndex++)
        {
            var partition = partitions[partitionIndex];
            starts[partitionIndex] = nextStart;
            lengths[partitionIndex] = partition.Count;

            for (var index = 0; index < partition.Count; index++)
                indices[nextStart + index] = partition[index];

            nextStart += partition.Count;
        }

        return new WindowPartitionSet(rowCount, indices, starts, lengths);
    }
}
