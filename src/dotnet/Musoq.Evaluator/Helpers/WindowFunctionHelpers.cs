using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Musoq.Plugins;

namespace Musoq.Evaluator.Helpers;

public static class WindowFunctionHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object CompositeKey(params object[] parts)
    {
        return parts.Length switch
        {
            0 => 0,
            1 => parts[0],
            _ => new CompositeKeyValue(parts)
        };
    }

    public static List<List<int>> ResolvePartitions(int rowCount, object[] partitionKeys)
    {
        if (partitionKeys == null)
            return [CreateSequentialIndices(rowCount)];

        return GroupByPartition(rowCount, partitionKeys);
    }

    public static List<List<int>> SortPartitions(
        List<List<int>> partitions, object[] orderKeys, bool[] orderDescending)
    {
        var sorted = new List<List<int>>(partitions.Count);
        for (var p = 0; p < partitions.Count; p++)
            sorted.Add(new List<int>(partitions[p]));

        SortAllPartitions(sorted, orderKeys, orderDescending);
        return sorted;
    }

    public static List<List<int>> ResolveSortedPartitions(
        int rowCount, object[] partitionKeys, object[] orderKeys, bool[] orderDescending)
    {
        var partitions = partitionKeys == null
            ? [CreateSequentialIndices(rowCount)]
            : GroupByPartition(rowCount, partitionKeys);

        SortAllPartitions(partitions, orderKeys, orderDescending);
        return partitions;
    }

    public static long[] ComputeRowNumber(int rowCount, List<List<int>> sortedPartitions)
    {
        var result = new long[rowCount];

        for (var p = 0; p < sortedPartitions.Count; p++)
        {
            var partition = sortedPartitions[p];
            long rowNum = 1;
            var count = partition.Count;
            for (var i = 0; i < count; i++)
                result[partition[i]] = rowNum++;
        }

        return result;
    }

    public static long[] ComputeRank(
        int rowCount, List<List<int>> sortedPartitions, object[] orderKeys)
    {
        if (orderKeys == null)
            return ComputeRowNumber(rowCount, sortedPartitions);

        var result = new long[rowCount];

        for (var p = 0; p < sortedPartitions.Count; p++)
        {
            var partition = sortedPartitions[p];
            long rank = 1;
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                if (i > 0 && !Equals(orderKeys[partition[i]], orderKeys[partition[i - 1]]))
                    rank = i + 1;

                result[partition[i]] = rank;
            }
        }

        return result;
    }

    public static long[] ComputeDenseRank(
        int rowCount, List<List<int>> sortedPartitions, object[] orderKeys)
    {
        if (orderKeys == null)
            return ComputeRowNumber(rowCount, sortedPartitions);

        var result = new long[rowCount];

        for (var p = 0; p < sortedPartitions.Count; p++)
        {
            var partition = sortedPartitions[p];
            long denseRank = 1;
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                if (i > 0 && !Equals(orderKeys[partition[i]], orderKeys[partition[i - 1]]))
                    denseRank++;

                result[partition[i]] = denseRank;
            }
        }

        return result;
    }

    public static object[] ComputeLag(
        int rowCount, List<List<int>> sortedPartitions,
        object[] values, int offset, object defaultValue)
    {
        var result = new object[rowCount];

        for (var p = 0; p < sortedPartitions.Count; p++)
        {
            var partition = sortedPartitions[p];
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                var lagIndex = i - offset;
                result[partition[i]] = lagIndex >= 0
                    ? values[partition[lagIndex]]
                    : defaultValue;
            }
        }

        return result;
    }

    public static object[] ComputeLead(
        int rowCount, List<List<int>> sortedPartitions,
        object[] values, int offset, object defaultValue)
    {
        var result = new object[rowCount];

        for (var p = 0; p < sortedPartitions.Count; p++)
        {
            var partition = sortedPartitions[p];
            var count = partition.Count;
            for (var i = 0; i < count; i++)
            {
                var leadIndex = i + offset;
                result[partition[i]] = leadIndex < count
                    ? values[partition[leadIndex]]
                    : defaultValue;
            }
        }

        return result;
    }

    public static object[] ComputePluginWindowFunction(
        int rowCount, List<List<int>> partitions,
        bool isSorted, object[] values, IWindowFunction windowFunction,
        object?[] extraArgs)
    {
        windowFunction.SetArguments(extraArgs);
        return ComputePluginWindowFunction(rowCount, partitions, isSorted, values, windowFunction);
    }

    public static object[] ComputePluginWindowFunction(
        int rowCount, List<List<int>> partitions,
        bool isSorted, object[] values, IWindowFunction windowFunction)
    {
        var result = new object[rowCount];

        for (var p = 0; p < partitions.Count; p++)
        {
            var partition = partitions[p];
            var count = partition.Count;
            windowFunction.SetPartitionSize(count);
            windowFunction.PartitionStart();

            if (isSorted)
            {
                for (var i = 0; i < count; i++)
                {
                    var idx = partition[i];
                    windowFunction.AccumulateValue(values[idx]);
                    result[idx] = windowFunction.GetCurrentValue()!;
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                    windowFunction.AccumulateValue(values[partition[i]]);

                var finalValue = windowFunction.GetCurrentValue()!;
                for (var i = 0; i < count; i++)
                    result[partition[i]] = finalValue;
            }
        }

        return result;
    }

    public static object[] ComputeWindowedAggregate(
        int rowCount, List<List<int>> partitions,
        bool isSorted, object[] values, string aggregateName)
    {
        var result = new object[rowCount];
        var aggType = ParseAggregateType(aggregateName);

        for (var p = 0; p < partitions.Count; p++)
        {
            var partition = partitions[p];

            if (isSorted)
            {
                ComputeRunningAggregate(partition, values, aggType, result);
            }
            else
            {
                var wholeValue = ComputeWholePartitionAggregate(partition, values, aggType);
                var count = partition.Count;
                for (var i = 0; i < count; i++)
                    result[partition[i]] = wholeValue;
            }
        }

        return result;
    }

    public static long[] ComputeRowNumber(
        int rowCount, object[] partitionKeys, object[] orderKeys, bool[] orderDescending)
    {
        var partitions = ResolvePartitions(rowCount, partitionKeys);
        var sorted = SortPartitions(partitions, orderKeys, orderDescending);
        return ComputeRowNumber(rowCount, sorted);
    }

    public static long[] ComputeRank(
        int rowCount, object[] partitionKeys, object[] orderKeys, bool[] orderDescending)
    {
        var partitions = ResolvePartitions(rowCount, partitionKeys);
        var sorted = SortPartitions(partitions, orderKeys, orderDescending);
        return ComputeRank(rowCount, sorted, orderKeys);
    }

    public static long[] ComputeDenseRank(
        int rowCount, object[] partitionKeys, object[] orderKeys, bool[] orderDescending)
    {
        var partitions = ResolvePartitions(rowCount, partitionKeys);
        var sorted = SortPartitions(partitions, orderKeys, orderDescending);
        return ComputeDenseRank(rowCount, sorted, orderKeys);
    }

    public static object[] ComputeLag(
        int rowCount, object[] partitionKeys, object[] orderKeys,
        bool[] orderDescending, object[] values, int offset, object defaultValue)
    {
        var partitions = ResolvePartitions(rowCount, partitionKeys);
        var sorted = SortPartitions(partitions, orderKeys, orderDescending);
        return ComputeLag(rowCount, sorted, values, offset, defaultValue);
    }

    public static object[] ComputeLead(
        int rowCount, object[] partitionKeys, object[] orderKeys,
        bool[] orderDescending, object[] values, int offset, object defaultValue)
    {
        var partitions = ResolvePartitions(rowCount, partitionKeys);
        var sorted = SortPartitions(partitions, orderKeys, orderDescending);
        return ComputeLead(rowCount, sorted, values, offset, defaultValue);
    }

    public static object[] ComputeWindowedAggregate(
        int rowCount, object[] partitionKeys, object[] orderKeys,
        bool hasOrderBy, bool[] orderDescending, object[] values, string aggregateName)
    {
        var partitions = ResolvePartitions(rowCount, partitionKeys);

        if (hasOrderBy)
        {
            var sorted = SortPartitions(partitions, orderKeys, orderDescending);
            return ComputeWindowedAggregate(rowCount, sorted, true, values, aggregateName);
        }

        return ComputeWindowedAggregate(rowCount, partitions, false, values, aggregateName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CompareValues(object a, object b, bool descending)
    {
        if (a is IComparable ca)
        {
            if (b is IComparable cb)
            {
                var cmp = ca.CompareTo(cb);
                return descending ? -cmp : cmp;
            }

            return descending ? -1 : 1;
        }

        if (b is IComparable)
            return descending ? 1 : -1;

        return 0;
    }

    private static List<int> CreateSequentialIndices(int rowCount)
    {
        var list = new List<int>(rowCount);
        for (var i = 0; i < rowCount; i++)
            list.Add(i);
        return list;
    }

    private static List<List<int>> GroupByPartition(int rowCount, object[] partitionKeys)
    {
        if (rowCount == 0)
            return [];

        var groups = new Dictionary<object, List<int>>(rowCount / 4 + 1, ObjectKeyComparer.Instance);
        for (var i = 0; i < rowCount; i++)
        {
            var key = partitionKeys[i] ?? DBNull.Value;
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<int>(rowCount / Math.Max(groups.Count, 1));
                groups[key] = list;
            }

            list.Add(i);
        }

        var result = new List<List<int>>(groups.Count);
        foreach (var kvp in groups)
            result.Add(kvp.Value);

        return result;
    }

    private static void SortAllPartitions(
        List<List<int>> partitions, object[] orderKeys, bool[] descendingFlags)
    {
        var sample = FindSampleKey(partitions, orderKeys);

        if (sample == null)
            return;

        if (sample is CompositeKeyValue)
        {
            foreach (var partition in partitions)
                SortCompositePartition(partition, orderKeys, descendingFlags);
            return;
        }

        var descending = descendingFlags.Length > 0 && descendingFlags[0];

        switch (sample)
        {
            case int:
                SortPartitionsTyped<int>(partitions, orderKeys, descending);
                break;
            case long:
                SortPartitionsTyped<long>(partitions, orderKeys, descending);
                break;
            case decimal:
                SortPartitionsTyped<decimal>(partitions, orderKeys, descending);
                break;
            case double:
                SortPartitionsTyped<double>(partitions, orderKeys, descending);
                break;
            case float:
                SortPartitionsTyped<float>(partitions, orderKeys, descending);
                break;
            case string:
                SortPartitionsTyped<string>(partitions, orderKeys, descending);
                break;
            case DateTime:
                SortPartitionsTyped<DateTime>(partitions, orderKeys, descending);
                break;
            default:
                SortPartitionsGeneric(partitions, orderKeys, descending);
                break;
        }
    }

    private static object FindSampleKey(List<List<int>> partitions, object[] orderKeys)
    {
        for (var p = 0; p < partitions.Count; p++)
        {
            var partition = partitions[p];
            for (var i = 0; i < partition.Count; i++)
            {
                var key = orderKeys[partition[i]];
                if (key != null)
                    return key;
            }
        }

        return null;
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

    private enum AggregateType
    {
        Sum,
        Count,
        Avg,
        Min,
        Max
    }

    private static AggregateType ParseAggregateType(string aggregateName)
    {
        return aggregateName.ToLowerInvariant() switch
        {
            "sum" => AggregateType.Sum,
            "count" => AggregateType.Count,
            "avg" => AggregateType.Avg,
            "min" => AggregateType.Min,
            "max" => AggregateType.Max,
            _ => throw new NotSupportedException($"Window aggregate function '{aggregateName}' is not supported.")
        };
    }

    private static void ComputeRunningAggregate(
        List<int> sortedIndices, object[] values,
        AggregateType aggregateType, object[] result)
    {
        switch (aggregateType)
        {
            case AggregateType.Sum:
                ComputeRunningSum(sortedIndices, values, result);
                break;
            case AggregateType.Count:
                ComputeRunningCount(sortedIndices, values, result);
                break;
            case AggregateType.Avg:
                ComputeRunningAvg(sortedIndices, values, result);
                break;
            case AggregateType.Min:
                ComputeRunningExtremum(sortedIndices, values, result, isMin: true);
                break;
            case AggregateType.Max:
                ComputeRunningExtremum(sortedIndices, values, result, isMin: false);
                break;
        }
    }

    private static void ComputeRunningSum(List<int> sortedIndices, object[] values, object[] result)
    {
        decimal runningSum = 0;
        var count = sortedIndices.Count;
        for (var i = 0; i < count; i++)
        {
            var idx = sortedIndices[i];
            var val = values[idx];
            if (val != null)
                runningSum += ToDecimalFast(val);
            result[idx] = runningSum;
        }
    }

    private static void ComputeRunningCount(List<int> sortedIndices, object[] values, object[] result)
    {
        var runningCount = 0;
        var count = sortedIndices.Count;
        for (var i = 0; i < count; i++)
        {
            var idx = sortedIndices[i];
            if (values[idx] != null)
                runningCount++;
            result[idx] = runningCount;
        }
    }

    private static void ComputeRunningAvg(List<int> sortedIndices, object[] values, object[] result)
    {
        decimal sum = 0;
        var runningCount = 0;
        var count = sortedIndices.Count;
        for (var i = 0; i < count; i++)
        {
            var idx = sortedIndices[i];
            var val = values[idx];
            if (val != null)
            {
                sum += ToDecimalFast(val);
                runningCount++;
            }

            result[idx] = runningCount > 0 ? sum / runningCount : 0m;
        }
    }

    private static void ComputeRunningExtremum(List<int> sortedIndices, object[] values, object[] result, bool isMin)
    {
        IComparable current = null;
        var count = sortedIndices.Count;
        for (var i = 0; i < count; i++)
        {
            var idx = sortedIndices[i];
            if (values[idx] is IComparable comparable)
            {
                if (current == null || (isMin ? comparable.CompareTo(current) < 0 : comparable.CompareTo(current) > 0))
                    current = comparable;
            }

            result[idx] = current;
        }
    }

    private static object ComputeWholePartitionAggregate(
        List<int> indices, object[] values, AggregateType aggregateType)
    {
        return aggregateType switch
        {
            AggregateType.Sum => ComputeWholeSum(indices, values),
            AggregateType.Count => ComputeWholeCount(indices, values),
            AggregateType.Avg => ComputeWholeAvg(indices, values),
            AggregateType.Min => ComputeWholeExtremum(indices, values, isMin: true),
            AggregateType.Max => ComputeWholeExtremum(indices, values, isMin: false),
            _ => throw new NotSupportedException($"Window aggregate function '{aggregateType}' is not supported.")
        };
    }

    private static object ComputeWholeSum(List<int> indices, object[] values)
    {
        decimal sum = 0;
        var count = indices.Count;
        for (var i = 0; i < count; i++)
        {
            var val = values[indices[i]];
            if (val != null)
                sum += ToDecimalFast(val);
        }

        return sum;
    }

    private static object ComputeWholeCount(List<int> indices, object[] values)
    {
        var count = 0;
        var total = indices.Count;
        for (var i = 0; i < total; i++)
        {
            if (values[indices[i]] != null)
                count++;
        }

        return count;
    }

    private static object ComputeWholeAvg(List<int> indices, object[] values)
    {
        decimal sum = 0;
        var count = 0;
        var total = indices.Count;
        for (var i = 0; i < total; i++)
        {
            var val = values[indices[i]];
            if (val != null)
            {
                sum += ToDecimalFast(val);
                count++;
            }
        }

        return count > 0 ? sum / count : 0m;
    }

    private static object ComputeWholeExtremum(List<int> indices, object[] values, bool isMin)
    {
        IComparable current = null;
        var count = indices.Count;
        for (var i = 0; i < count; i++)
        {
            if (values[indices[i]] is IComparable comparable)
            {
                if (current == null || (isMin ? comparable.CompareTo(current) < 0 : comparable.CompareTo(current) > 0))
                    current = comparable;
            }
        }

        return current;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static decimal ToDecimalFast(object value)
    {
        return value switch
        {
            int i => i,
            long l => l,
            decimal d => d,
            double dbl => (decimal)dbl,
            float f => (decimal)f,
            short s => s,
            byte b => b,
            _ => Convert.ToDecimal(value)
        };
    }

    private sealed class ObjectKeyComparer : IEqualityComparer<object>
    {
        public static readonly ObjectKeyComparer Instance = new();

        bool IEqualityComparer<object>.Equals(object x, object y) => Equals(x, y);

        public int GetHashCode(object obj) => obj?.GetHashCode() ?? 0;
    }

    internal sealed class CompositeKeyValue : IEquatable<CompositeKeyValue>, IComparable<CompositeKeyValue>, IComparable
    {
        private readonly object[] _parts;
        private readonly int _hashCode;

        public CompositeKeyValue(object[] parts)
        {
            _parts = parts;
            var hash = new HashCode();
            for (var i = 0; i < parts.Length; i++)
                hash.Add(parts[i]);
            _hashCode = hash.ToHashCode();
        }

        public bool Equals(CompositeKeyValue other)
        {
            if (other == null || _parts.Length != other._parts.Length)
                return false;

            if (_hashCode != other._hashCode)
                return false;

            for (var i = 0; i < _parts.Length; i++)
            {
                if (!Equals(_parts[i], other._parts[i]))
                    return false;
            }

            return true;
        }

        public int CompareTo(CompositeKeyValue other)
        {
            return CompareTo(other, []);
        }

        public int CompareTo(CompositeKeyValue other, bool[] descendingFlags)
        {
            if (other == null) return 1;

            var len = Math.Min(_parts.Length, other._parts.Length);
            for (var i = 0; i < len; i++)
            {
                var descending = i < descendingFlags.Length && descendingFlags[i];
                var ca = _parts[i] as IComparable;
                var cb = other._parts[i] as IComparable;

                if (ca == null && cb == null) continue;
                if (ca == null) return descending ? 1 : -1;
                if (cb == null) return descending ? -1 : 1;

                var cmp = ca.CompareTo(cb);
                if (cmp != 0)
                    return descending ? -cmp : cmp;
            }

            return 0;
        }

        int IComparable.CompareTo(object obj)
        {
            if (obj is CompositeKeyValue other)
                return CompareTo(other);
            return 1;
        }

        public override bool Equals(object obj) => obj is CompositeKeyValue other && Equals(other);

        public override int GetHashCode() => _hashCode;
    }
}
