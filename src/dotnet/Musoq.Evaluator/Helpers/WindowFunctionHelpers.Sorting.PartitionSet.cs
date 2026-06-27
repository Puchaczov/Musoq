using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    private static void SortPartitionSetObjectInPlace(
        WindowPartitionSet partitions, object[] orderKeys, bool[] descendingFlags)
    {
        var sample = FindSampleKey(partitions, orderKeys);

        if (sample == null)
            return;

        var comparer = sample is CompositeKeyValue
            ? new CompositePartitionSetComparer(orderKeys, descendingFlags)
            : CreateObjectPartitionSetComparer(sample, orderKeys, descendingFlags);

        SortPartitionSetWithComparer(partitions, comparer);
    }

    private static void SortPartitionSetTypedDirectInPlace<T>(
        WindowPartitionSet partitions, T[] orderKeys, bool descending)
        where T : IComparable<T>
    {
        if (descending)
        {
            SortPartitionSetByDescendingKeys(partitions, orderKeys);
            return;
        }

        SortPartitionSetByAscendingKeys(partitions, orderKeys);
    }

    private static void SortPartitionSetWithComparer(
        WindowPartitionSet partitions, IComparer<int> comparer)
    {
        var indices = partitions.Indices;
        for (var partitionIndex = 0; partitionIndex < partitions.PartitionCount; partitionIndex++)
        {
            var count = partitions.GetLength(partitionIndex);
            if (count <= 1)
                continue;

            Array.Sort(indices, partitions.GetStart(partitionIndex), count, comparer);
        }
    }
}
