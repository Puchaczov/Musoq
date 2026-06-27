using System.Buffers;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    private static void SortPartitionSetByIntKeys(WindowPartitionSet partitions, int[] orderKeys, bool descending)
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
                    var sortKey = (uint)(orderKeys[indices[start + index]] ^ int.MinValue);
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

    private static void SortPartitionSetByAscendingKeys<T>(WindowPartitionSet partitions, T[] orderKeys)
        where T : IComparable<T>
    {
        var indices = partitions.Indices;
        var sortKeys = ArrayPool<AscendingPartitionSortKey<T>>.Shared.Rent(indices.Length);

        try
        {
            for (var partitionIndex = 0; partitionIndex < partitions.PartitionCount; partitionIndex++)
            {
                var start = partitions.GetStart(partitionIndex);
                var count = partitions.GetLength(partitionIndex);
                if (count <= 1)
                    continue;

                for (var index = 0; index < count; index++)
                    sortKeys[start + index] = new AscendingPartitionSortKey<T>(orderKeys[indices[start + index]]);

                Array.Sort(sortKeys, indices, start, count);
            }
        }
        finally
        {
            ArrayPool<AscendingPartitionSortKey<T>>.Shared.Return(sortKeys, RuntimeHelpers.IsReferenceOrContainsReferences<AscendingPartitionSortKey<T>>());
        }
    }

    private static void SortPartitionSetByDescendingKeys<T>(WindowPartitionSet partitions, T[] orderKeys)
        where T : IComparable<T>
    {
        var indices = partitions.Indices;
        var sortKeys = ArrayPool<DescendingPartitionSortKey<T>>.Shared.Rent(indices.Length);

        try
        {
            for (var partitionIndex = 0; partitionIndex < partitions.PartitionCount; partitionIndex++)
            {
                var start = partitions.GetStart(partitionIndex);
                var count = partitions.GetLength(partitionIndex);
                if (count <= 1)
                    continue;

                for (var index = 0; index < count; index++)
                    sortKeys[start + index] = new DescendingPartitionSortKey<T>(orderKeys[indices[start + index]]);

                Array.Sort(sortKeys, indices, start, count);
            }
        }
        finally
        {
            ArrayPool<DescendingPartitionSortKey<T>>.Shared.Return(sortKeys, RuntimeHelpers.IsReferenceOrContainsReferences<DescendingPartitionSortKey<T>>());
        }
    }
}
