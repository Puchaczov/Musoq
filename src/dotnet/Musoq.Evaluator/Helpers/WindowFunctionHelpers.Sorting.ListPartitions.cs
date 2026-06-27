using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    private static List<List<int>> ResolveSortedUnpartitioned<TOrder>(
        int rowCount, TOrder[] orderKeys, bool[] orderDescending)
        where TOrder : IComparable<TOrder>
    {
        var sorted = new List<List<int>> { CreateSequentialIndices(rowCount) };
        var descending = orderDescending.Length > 0 && orderDescending[0];
        SortPartitionsTypedDirect(sorted, orderKeys, descending);
        return sorted;
    }

    private static void SortPartitionsTyped<T>(
        List<List<int>> partitions, object[] orderKeys, bool descending)
        where T : IComparable<T>
    {
        foreach (var partition in partitions)
        {
            if (partition.Count <= 1)
                continue;

            if (typeof(T).IsValueType)
            {
                if (descending)
                    partition.Sort((a, b) => ((T)orderKeys[b]).CompareTo((T)orderKeys[a]));
                else
                    partition.Sort((a, b) => ((T)orderKeys[a]).CompareTo((T)orderKeys[b]));
            }
            else
            {
                if (descending)
                    partition.Sort((a, b) =>
                    {
                        var ka = orderKeys[a];
                        var kb = orderKeys[b];
                        if (ka == null) return kb == null ? 0 : 1;
                        if (kb == null) return -1;
                        return ((T)kb).CompareTo((T)ka);
                    });
                else
                    partition.Sort((a, b) =>
                    {
                        var ka = orderKeys[a];
                        var kb = orderKeys[b];
                        if (ka == null) return kb == null ? 0 : -1;
                        if (kb == null) return 1;
                        return ((T)ka).CompareTo((T)kb);
                    });
            }
        }
    }

    private static void SortPartitionsTypedDirect<T>(
        List<List<int>> partitions, T[] orderKeys, bool descending)
        where T : IComparable<T>
    {
        foreach (var partition in partitions)
        {
            if (partition.Count <= 1)
                continue;

            if (typeof(T).IsValueType)
            {
                if (descending)
                    partition.Sort((a, b) => orderKeys[b].CompareTo(orderKeys[a]));
                else
                    partition.Sort((a, b) => orderKeys[a].CompareTo(orderKeys[b]));
            }
            else
            {
                if (descending)
                    partition.Sort((a, b) =>
                    {
                        var ka = orderKeys[a];
                        var kb = orderKeys[b];
                        if (ka == null) return kb == null ? 0 : 1;
                        if (kb == null) return -1;
                        return kb.CompareTo(ka);
                    });
                else
                    partition.Sort((a, b) =>
                    {
                        var ka = orderKeys[a];
                        var kb = orderKeys[b];
                        if (ka == null) return kb == null ? 0 : -1;
                        if (kb == null) return 1;
                        return ka.CompareTo(kb);
                    });
            }
        }
    }

    private static void SortCompositePartition(
        List<int> indices, object[] orderKeys, bool[] descendingFlags)
    {
        if (indices.Count <= 1)
            return;

        indices.Sort((a, b) =>
            ((CompositeKeyValue)orderKeys[a]).CompareTo(
                (CompositeKeyValue)orderKeys[b], descendingFlags));
    }

    private static void SortPartitionsGeneric(
        List<List<int>> partitions, object[] orderKeys, bool descending)
    {
        foreach (var partition in partitions)
        {
            if (partition.Count <= 1)
                continue;

            if (descending)
                partition.Sort((a, b) =>
                {
                    if (orderKeys[a] is IComparable ca)
                        return orderKeys[b] is IComparable cb ? -ca.CompareTo(cb) : -1;
                    return orderKeys[b] is IComparable ? 1 : 0;
                });
            else
                partition.Sort((a, b) =>
                {
                    if (orderKeys[a] is IComparable ca)
                        return orderKeys[b] is IComparable cb ? ca.CompareTo(cb) : 1;
                    return orderKeys[b] is IComparable ? -1 : 0;
                });
        }
    }
}
