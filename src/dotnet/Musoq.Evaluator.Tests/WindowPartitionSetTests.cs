using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class WindowPartitionSetTests
{
    [TestMethod]
    public void ResolvePartitionSet_WhenPartitionKeysAreTyped_ShouldMatchLegacyPartitions()
    {
        string?[] partitionKeys = ["b", "a", "b", null, "a", null, "c"];

        var expected = WindowFunctionHelpers.ResolvePartitions(partitionKeys.Length, partitionKeys);
        var actual = WindowFunctionHelpers.ResolvePartitionSet(partitionKeys.Length, partitionKeys);

        AssertPartitionsEqual(expected, actual);
    }

    [TestMethod]
    public void ResolvePartitionSet_WhenPartitionKeysAreObjects_ShouldMatchLegacyPartitions()
    {
        object?[] partitionKeys = ["b", 1, "b", null, 1, null, DateTime.UnixEpoch];

        var expected = WindowFunctionHelpers.ResolvePartitions(partitionKeys.Length, partitionKeys);
        var actual = WindowFunctionHelpers.ResolvePartitionSet(partitionKeys.Length, partitionKeys);

        AssertPartitionsEqual(expected, actual);
    }

    [TestMethod]
    public void ResolvePartitionSet_WhenPartitionKeysAreValueType_ShouldGroupTypedKeys()
    {
        var partitionKeys = new DepartmentKey[]
        {
            new(2),
            new(1),
            new(2),
            new(3),
            new(1)
        };

        var actual = WindowFunctionHelpers.ResolvePartitionSet(partitionKeys.Length, partitionKeys);

        AssertPartitionsEqual([[0, 2], [1, 4], [3]], actual);
    }

    [TestMethod]
    public void ResolvePartitionSet_WhenUnpartitioned_ShouldKeepSingleSequentialPartition()
    {
        var expected = WindowFunctionHelpers.ResolvePartitions(5, null);
        var actual = WindowFunctionHelpers.ResolvePartitionSet(5, null);

        AssertPartitionsEqual(expected, actual);
    }

    [TestMethod]
    public void SortPartitionSet_WhenOrderKeyIsTyped_ShouldMatchLegacySort()
    {
        string?[] partitionKeys = ["b", "a", "b", null, "a", null, "c"];
        int[] orderKeys = [20, 10, 30, 5, 10, 7, 1];
        bool[] descending = [true];

        var expectedPartitions = WindowFunctionHelpers.ResolvePartitions(partitionKeys.Length, partitionKeys);
        var expected = WindowFunctionHelpers.SortPartitions(expectedPartitions, orderKeys, descending);
        var actualPartitions = WindowFunctionHelpers.ResolvePartitionSet(partitionKeys.Length, partitionKeys);
        var actual = WindowFunctionHelpers.SortPartitionSet(actualPartitions, orderKeys, descending);

        AssertPartitionsEqual(expected, actual);
    }

    [TestMethod]
    public void SortPartitionSet_WhenOrderKeyIsComposite_ShouldMatchLegacySort()
    {
        object[] partitionKeys = ["x", "x", "x", "y", "y"];
        object[] orderKeys =
        [
            WindowFunctionHelpers.CompositeKey("b", 2)!,
            WindowFunctionHelpers.CompositeKey("a", 3)!,
            WindowFunctionHelpers.CompositeKey("a", 1)!,
            WindowFunctionHelpers.CompositeKey("c", 1)!,
            WindowFunctionHelpers.CompositeKey("b", 5)!
        ];
        bool[] descending = [false, true];

        var expectedPartitions = WindowFunctionHelpers.ResolvePartitions(partitionKeys.Length, partitionKeys);
        var expected = WindowFunctionHelpers.SortPartitions(expectedPartitions, orderKeys, descending);
        var actualPartitions = WindowFunctionHelpers.ResolvePartitionSet(partitionKeys.Length, partitionKeys);
        var actual = WindowFunctionHelpers.SortPartitionSet(actualPartitions, orderKeys, descending);

        AssertPartitionsEqual(expected, actual);
    }

    [TestMethod]
    public void SortStructPartitionSetInPlace_WhenOrderKeyIsGeneratedStyleStruct_ShouldSortAscendingAndDescending()
    {
        string[] partitionKeys = ["a", "a", "a", "b", "b"];
        var orderKeys = new ScoreKey[]
        {
            new(20),
            new(10),
            new(30),
            new(2),
            new(1)
        };

        var ascending = WindowFunctionHelpers.ResolvePartitionSet(partitionKeys.Length, partitionKeys);
        WindowFunctionHelpers.SortStructPartitionSetInPlace(ascending, orderKeys, false);

        AssertPartitionsEqual([[1, 0, 2], [4, 3]], ascending);

        var descending = WindowFunctionHelpers.ResolvePartitionSet(partitionKeys.Length, partitionKeys);
        WindowFunctionHelpers.SortStructPartitionSetInPlace(descending, orderKeys, true);

        AssertPartitionsEqual([[2, 0, 1], [3, 4]], descending);
    }

    [TestMethod]
    public void RankFunctions_WhenOrderKeyIsGeneratedStyleStruct_ShouldUseTypedPeerEquality()
    {
        string[] partitionKeys = ["a", "a", "a", "a"];
        var orderKeys = new ScoreKey[]
        {
            new(20),
            new(10),
            new(20),
            new(5)
        };
        var sorted = WindowFunctionHelpers.ResolvePartitionSet(partitionKeys.Length, partitionKeys);
        WindowFunctionHelpers.SortStructPartitionSetInPlace(sorted, orderKeys, true);

        CollectionAssert.AreEqual(
            new long[] { 1, 3, 1, 4 },
            WindowFunctionHelpers.ComputeRank(partitionKeys.Length, sorted, orderKeys));
        CollectionAssert.AreEqual(
            new long[] { 1, 2, 1, 3 },
            WindowFunctionHelpers.ComputeDenseRank(partitionKeys.Length, sorted, orderKeys));
    }

    [TestMethod]
    public void SortPartitionSet_WhenReferenceOrderKeysContainNulls_ShouldPreserveNullOrdering()
    {
        string[] partitionKeys = ["a", "a", "a", "a"];
        string[] orderKeys = ["b", null!, "a", null!];

        var ascending = WindowFunctionHelpers.ResolvePartitionSet(partitionKeys.Length, partitionKeys);
        WindowFunctionHelpers.SortPartitionSetInPlace(ascending, orderKeys, false);

        AssertPartitionsEqual([[1, 3, 2, 0]], ascending);

        var descending = WindowFunctionHelpers.ResolvePartitionSet(partitionKeys.Length, partitionKeys);
        WindowFunctionHelpers.SortPartitionSetInPlace(descending, orderKeys, true);

        AssertPartitionsEqual([[0, 2, 1, 3]], descending);
    }

    [TestMethod]
    public void RankingFunctions_WhenUsingPartitionSet_ShouldMatchLegacyResults()
    {
        string?[] partitionKeys = ["eng", "eng", "sales", "eng", "sales", null, null];
        int[] orderKeys = [200, 200, 100, 150, 120, 10, 10];
        var rowCount = partitionKeys.Length;

        var legacyPartitions = WindowFunctionHelpers.ResolvePartitions(rowCount, partitionKeys);
        var legacySorted = WindowFunctionHelpers.SortPartitions(legacyPartitions, orderKeys, [true]);
        var compactPartitions = WindowFunctionHelpers.ResolvePartitionSet(rowCount, partitionKeys);
        var compactSorted = WindowFunctionHelpers.SortPartitionSet(compactPartitions, orderKeys, [true]);

        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeRowNumber(rowCount, legacySorted),
            WindowFunctionHelpers.ComputeRowNumber(rowCount, compactSorted));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeRowNumberTopN(rowCount, legacySorted, 2),
            WindowFunctionHelpers.ComputeRowNumberTopN(rowCount, compactSorted, 2));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeRank(rowCount, legacySorted, orderKeys),
            WindowFunctionHelpers.ComputeRank(rowCount, compactSorted, orderKeys));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeRankTopN(rowCount, legacySorted, orderKeys, 2),
            WindowFunctionHelpers.ComputeRankTopN(rowCount, compactSorted, orderKeys, 2));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeDenseRank(rowCount, legacySorted, orderKeys),
            WindowFunctionHelpers.ComputeDenseRank(rowCount, compactSorted, orderKeys));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeDenseRankTopN(rowCount, legacySorted, orderKeys, 2),
            WindowFunctionHelpers.ComputeDenseRankTopN(rowCount, compactSorted, orderKeys, 2));
    }

    [TestMethod]
    public void OffsetFunctions_WhenUsingPartitionSet_ShouldMatchLegacyResults()
    {
        string?[] partitionKeys = ["eng", "eng", "sales", "eng", "sales", null, null];
        int[] orderKeys = [200, 200, 100, 150, 120, 10, 10];
        int[] values = [1, 2, 3, 4, 5, 6, 7];
        int[] offsets = [1, 2, 1, 1, 2, 1, 2];
        object[] defaults = ["d0", "d1", "d2", "d3", "d4", "d5", "d6"];
        var rowCount = partitionKeys.Length;

        var legacyPartitions = WindowFunctionHelpers.ResolvePartitions(rowCount, partitionKeys);
        var legacySorted = WindowFunctionHelpers.SortPartitions(legacyPartitions, orderKeys, [true]);
        var compactPartitions = WindowFunctionHelpers.ResolvePartitionSet(rowCount, partitionKeys);
        var compactSorted = WindowFunctionHelpers.SortPartitionSet(compactPartitions, orderKeys, [true]);

        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeLag(rowCount, legacySorted, values, 1, null),
            WindowFunctionHelpers.ComputeLag(rowCount, compactSorted, values, 1, null));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeLag(rowCount, legacySorted, values, offsets, defaults),
            WindowFunctionHelpers.ComputeLag(rowCount, compactSorted, values, offsets, defaults));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeLead(rowCount, legacySorted, values, 1, null),
            WindowFunctionHelpers.ComputeLead(rowCount, compactSorted, values, 1, null));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeLead(rowCount, legacySorted, values, offsets, defaults),
            WindowFunctionHelpers.ComputeLead(rowCount, compactSorted, values, offsets, defaults));
    }

    [TestMethod]
    public void WindowAggregate_WhenUsingPartitionSet_ShouldMatchLegacyResults()
    {
        string?[] partitionKeys = ["eng", "eng", "sales", "eng", "sales", null, null];
        int[] orderKeys = [200, 200, 100, 150, 120, 10, 10];
        object?[] values = [10m, null, 30m, 40m, 50m, 60m, 70m];
        var rowCount = partitionKeys.Length;

        var legacyPartitions = WindowFunctionHelpers.ResolvePartitions(rowCount, partitionKeys);
        var legacySorted = WindowFunctionHelpers.SortPartitions(legacyPartitions, orderKeys, [false]);
        var compactPartitions = WindowFunctionHelpers.ResolvePartitionSet(rowCount, partitionKeys);
        var compactSorted = WindowFunctionHelpers.SortPartitionSet(compactPartitions, orderKeys, [false]);

        foreach (var aggregate in new[] { "sum", "count", "avg", "min", "max" })
        {
            CollectionAssert.AreEqual(
                AsObjectArray(WindowFunctionHelpers.ComputeWindowedAggregate(rowCount, legacyPartitions, false, values, aggregate)),
                AsObjectArray(WindowFunctionHelpers.ComputeWindowedAggregate(rowCount, compactPartitions, false, values, aggregate)),
                aggregate);
            CollectionAssert.AreEqual(
                AsObjectArray(WindowFunctionHelpers.ComputeWindowedAggregate(rowCount, legacySorted, true, values, aggregate)),
                AsObjectArray(WindowFunctionHelpers.ComputeWindowedAggregate(rowCount, compactSorted, true, values, aggregate)),
                aggregate);
        }
    }

    [TestMethod]
    public void PluginWindowFunction_WhenUsingPartitionSet_ShouldMatchLegacyResults()
    {
        string[] partitionKeys = ["a", "a", "b", "a", "b"];
        int[] orderKeys = [1, 3, 2, 2, 1];
        object[] values = [10m, 20m, 30m, 40m, 50m];
        var rowCount = partitionKeys.Length;

        var legacyPartitions = WindowFunctionHelpers.ResolvePartitions(rowCount, partitionKeys);
        var legacySorted = WindowFunctionHelpers.SortPartitions(legacyPartitions, orderKeys, [false]);
        var compactPartitions = WindowFunctionHelpers.ResolvePartitionSet(rowCount, partitionKeys);
        var compactSorted = WindowFunctionHelpers.SortPartitionSet(compactPartitions, orderKeys, [false]);

        CollectionAssert.AreEqual(
            new object[] { 10m, 70m, 80m, 50m, 50m },
            WindowFunctionHelpers.ComputePluginWindowFunction(
                rowCount, compactSorted, true, values, new SumWindowFunction()));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputePluginWindowFunction(
                rowCount, legacySorted, true, values, new SumWindowFunction()),
            WindowFunctionHelpers.ComputePluginWindowFunction(
                rowCount, compactSorted, true, values, new SumWindowFunction()));
        CollectionAssert.AreEqual(
            new object[] { 70m, 70m, 80m, 70m, 80m },
            WindowFunctionHelpers.ComputePluginWindowFunction(
                rowCount, compactPartitions, false, values, new SumWindowFunction()));
    }

    [TestMethod]
    public void FramedPluginWindowFunction_WhenUsingPartitionSet_ShouldMatchLegacyResults()
    {
        string[] partitionKeys = ["a", "a", "b", "a", "b"];
        int[] orderKeys = [1, 3, 2, 2, 1];
        object[] values = [10m, 20m, 30m, 40m, 50m];
        var frame = new FrameBounds(
            new FrameBound(FrameBoundKind.OffsetPreceding, 1),
            new FrameBound(FrameBoundKind.CurrentRow));
        var rowCount = partitionKeys.Length;

        var legacyPartitions = WindowFunctionHelpers.ResolvePartitions(rowCount, partitionKeys);
        var legacySorted = WindowFunctionHelpers.SortPartitions(legacyPartitions, orderKeys, [false]);
        var compactPartitions = WindowFunctionHelpers.ResolvePartitionSet(rowCount, partitionKeys);
        var compactSorted = WindowFunctionHelpers.SortPartitionSet(compactPartitions, orderKeys, [false]);

        CollectionAssert.AreEqual(
            new object[] { 10m, 60m, 80m, 50m, 50m },
            WindowFunctionHelpers.ComputeFramedPluginWindowFunction(
                rowCount, compactSorted, values, new SumWindowFunction(), frame));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeFramedPluginWindowFunction(
                rowCount, legacySorted, values, new SumWindowFunction(), frame),
            WindowFunctionHelpers.ComputeFramedPluginWindowFunction(
                rowCount, compactSorted, values, new SumWindowFunction(), frame));
    }

    private static void AssertPartitionsEqual(List<List<int>> expected, WindowPartitionSet actual)
    {
        var actualLists = actual.ToLists();

        Assert.HasCount(expected.Count, actualLists);
        for (var index = 0; index < expected.Count; index++)
            CollectionAssert.AreEqual(expected[index], actualLists[index], $"partition {index}");

        Assert.HasCount(expected.Sum(static partition => partition.Count), actual.Indices);
    }

    private static object[] AsObjectArray(object?[] values)
    {
        return values.Select(static value => value!).ToArray();
    }

    private sealed class SumWindowFunction : IWindowFunction
    {
        private decimal _sum;

        public void PartitionStart()
        {
            _sum = 0m;
        }

        public void AccumulateValue(object? value)
        {
            if (value != null)
                _sum += Convert.ToDecimal(value);
        }

        public object GetCurrentValue()
        {
            return _sum;
        }
    }

    private readonly struct DepartmentKey(int value) : IEquatable<DepartmentKey>
    {
        private readonly int _value = value;

        public bool Equals(DepartmentKey other)
        {
            return _value == other._value;
        }

        public override bool Equals(object? obj)
        {
            return obj is DepartmentKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value;
        }
    }

    private readonly struct ScoreKey(int value) : IComparable<ScoreKey>, IEquatable<ScoreKey>
    {
        private readonly int _value = value;

        public int CompareTo(ScoreKey other)
        {
            return _value.CompareTo(other._value);
        }

        public bool Equals(ScoreKey other)
        {
            return _value == other._value;
        }

        public override bool Equals(object? obj)
        {
            return obj is ScoreKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value;
        }
    }
}
