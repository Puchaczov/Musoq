using System.Buffers;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static WindowPartitionSet SortStructPartitionSet<T>(
        WindowPartitionSet partitions, T[] orderKeys, bool descending)
        where T : struct, IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(partitions);
        var sorted = partitions.Copy();
        return SortStructPartitionSetInPlace(sorted, orderKeys, descending);
    }

    public static WindowPartitionSet SortStructPartitionSetInPlace<T>(
        WindowPartitionSet partitions, T[] orderKeys, bool descending)
        where T : struct, IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(partitions);
        ArgumentNullException.ThrowIfNull(orderKeys);
        SortPartitionSetByStructKeys(partitions, orderKeys, descending);
        return partitions;
    }

    private static void SortPartitionSetByStructKeys<T>(
        WindowPartitionSet partitions, T[] orderKeys, bool descending)
        where T : struct, IComparable<T>
    {
        var indices = partitions.Indices;
        var sortKeys = ArrayPool<StructPartitionSortKey<T>>.Shared.Rent(indices.Length);

        try
        {
            for (var partitionIndex = 0; partitionIndex < partitions.PartitionCount; partitionIndex++)
            {
                var start = partitions.GetStart(partitionIndex);
                var count = partitions.GetLength(partitionIndex);
                if (count <= 1)
                    continue;

                for (var index = 0; index < count; index++)
                    sortKeys[start + index] = new StructPartitionSortKey<T>(orderKeys[indices[start + index]], descending);

                Array.Sort(sortKeys, indices, start, count);
            }
        }
        finally
        {
            ArrayPool<StructPartitionSortKey<T>>.Shared.Return(sortKeys, RuntimeHelpers.IsReferenceOrContainsReferences<StructPartitionSortKey<T>>());
        }
    }

    private readonly struct StructPartitionSortKey<T>(T value, bool descending) : IComparable<StructPartitionSortKey<T>>
        where T : struct, IComparable<T>
    {
        private readonly T _value = value;
        private readonly bool _descending = descending;

        public int CompareTo(StructPartitionSortKey<T> other)
        {
            var comparison = _value.CompareTo(other._value);
            return _descending ? -comparison : comparison;
        }
    }
}
