using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Helpers;

public readonly struct WindowPartitionCountResult
{
    private readonly int[] _rowPartitions;
    private readonly int[] _counts;

    internal WindowPartitionCountResult(int[] rowPartitions, int[] counts)
    {
        _rowPartitions = rowPartitions;
        _counts = counts;
    }

    public int this[int rowIndex]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _counts[_rowPartitions[rowIndex]];
    }
}
