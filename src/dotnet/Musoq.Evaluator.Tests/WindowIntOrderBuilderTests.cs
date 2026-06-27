using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class WindowIntOrderBuilderTests
{
    [TestMethod]
    public void PartitionedBuilder_WhenSortedDescending_ShouldMatchPartitionSetRankingResults()
    {
        string?[] partitionKeys = ["eng", "eng", "sales", "eng", "sales", null, null];
        int[] orderKeys = [200, 200, 100, 150, 120, 10, 10];
        var rowCount = partitionKeys.Length;
        var builder = CreatePartitionedBuilder(partitionKeys, orderKeys);

        var expected = WindowFunctionHelpers.ResolvePartitionSet(rowCount, partitionKeys);
        WindowFunctionHelpers.SortPartitionSetInPlace(expected, orderKeys, true);

        AssertPartitionsEqual(expected, builder.ToSortedPartitionSet(true));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeRowNumber(rowCount, expected),
            builder.ComputeRowNumber(true));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeRank(rowCount, expected, orderKeys),
            builder.ComputeRank(true));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeDenseRank(rowCount, expected, orderKeys),
            builder.ComputeDenseRank(true));
    }

    [TestMethod]
    public void PartitionedBuilder_WhenSortedAscending_ShouldMatchPartitionSetRankingResults()
    {
        string?[] partitionKeys = ["eng", "eng", "sales", "eng", "sales", null, null];
        int[] orderKeys = [200, 200, 100, 150, 120, 10, 10];
        var rowCount = partitionKeys.Length;
        var builder = CreatePartitionedBuilder(partitionKeys, orderKeys);

        var expected = WindowFunctionHelpers.ResolvePartitionSet(rowCount, partitionKeys);
        WindowFunctionHelpers.SortPartitionSetInPlace(expected, orderKeys, false);

        AssertPartitionsEqual(expected, builder.ToSortedPartitionSet(false));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeRankTopN(rowCount, expected, orderKeys, 2),
            builder.ComputeRankTopN(false, 2));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeDenseRankTopN(rowCount, expected, orderKeys, 2),
            builder.ComputeDenseRankTopN(false, 2));
    }

    [TestMethod]
    public void PartitionedBuilder_WhenDuplicateOrderKeys_ShouldAssignRankAndDenseRankPeers()
    {
        string?[] partitionKeys = ["eng", "eng", "eng", "eng", "eng"];
        int[] orderKeys = [30, 20, 20, 10, 30];
        var builder = CreatePartitionedBuilder(partitionKeys, orderKeys);

        CollectionAssert.AreEqual(
            new long[] { 1, 3, 3, 5, 1 },
            builder.ComputeRank(true));
        CollectionAssert.AreEqual(
            new long[] { 1, 2, 2, 3, 1 },
            builder.ComputeDenseRank(true));
    }

    [TestMethod]
    public void PartitionedBuilder_WhenMultiplePartitions_ShouldRankWithinEachPartition()
    {
        string?[] partitionKeys = ["eng", "sales", "eng", "sales", "eng"];
        int[] orderKeys = [10, 10, 20, 5, 20];
        var builder = CreatePartitionedBuilder(partitionKeys, orderKeys);

        CollectionAssert.AreEqual(
            new long[] { 3, 1, 1, 2, 1 },
            builder.ComputeRank(true));
        CollectionAssert.AreEqual(
            new long[] { 2, 1, 1, 2, 1 },
            builder.ComputeDenseRank(true));
    }

    [TestMethod]
    public void PartitionedBuilder_WhenNullPartitionKeys_ShouldRankNullPartitionSeparately()
    {
        string?[] partitionKeys = [null, "eng", null, "eng", null];
        int[] orderKeys = [20, 10, 20, 30, 5];
        var builder = CreatePartitionedBuilder(partitionKeys, orderKeys);

        CollectionAssert.AreEqual(
            new long[] { 1, 2, 1, 1, 3 },
            builder.ComputeRank(true));
        CollectionAssert.AreEqual(
            new long[] { 1, 2, 1, 1, 2 },
            builder.ComputeDenseRank(true));
    }

    [TestMethod]
    public void PartitionedBuilder_WhenTopNGuardsApplied_ShouldLeaveRowsBeyondRankAsDefault()
    {
        string?[] partitionKeys = ["eng", "eng", "eng", "eng", "eng"];
        int[] orderKeys = [30, 20, 20, 10, 30];
        var builder = CreatePartitionedBuilder(partitionKeys, orderKeys);

        CollectionAssert.AreEqual(
            new long[] { 1, 0, 0, 0, 1 },
            builder.ComputeRankTopN(true, 2));
        CollectionAssert.AreEqual(
            new long[] { 1, 2, 2, 0, 1 },
            builder.ComputeDenseRankTopN(true, 2));
    }

    [TestMethod]
    public void PartitionedBuilder_WhenRequestedTwice_ShouldReuseSortedPartitionSet()
    {
        string[] partitionKeys = ["eng", "eng", "sales", "eng", "sales"];
        int[] orderKeys = [200, 150, 100, 175, 120];
        var builder = CreatePartitionedBuilder(partitionKeys, orderKeys);

        var first = builder.ToSortedPartitionSet(true);
        var second = builder.ToSortedPartitionSet(true);

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void UnpartitionedBuilder_WhenSortedDescending_ShouldMatchPartitionSetRankingResults()
    {
        int[] orderKeys = [3, 8, 8, -1, 5, 3];
        var rowCount = orderKeys.Length;
        var builder = new WindowIntOrderBuilder(rowCount);
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            builder.Add(orderKeys[rowIndex], rowIndex);

        var expected = WindowFunctionHelpers.ResolvePartitionSet(rowCount, null);
        WindowFunctionHelpers.SortPartitionSetInPlace(expected, orderKeys, true);

        AssertPartitionsEqual(expected, builder.ToSortedPartitionSet(true));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeRowNumberTopN(rowCount, expected, 3),
            builder.ComputeRowNumberTopN(true, 3));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeRank(rowCount, expected, orderKeys),
            builder.ComputeRank(true));
        CollectionAssert.AreEqual(
            WindowFunctionHelpers.ComputeDenseRank(rowCount, expected, orderKeys),
            builder.ComputeDenseRank(true));
    }

    private static WindowIntOrderBuilder<string> CreatePartitionedBuilder(string?[] partitionKeys, int[] orderKeys)
    {
        var builder = new WindowIntOrderBuilder<string>(partitionKeys.Length);
        for (var rowIndex = 0; rowIndex < partitionKeys.Length; rowIndex++)
            builder.Add(partitionKeys[rowIndex], orderKeys[rowIndex], rowIndex);

        return builder;
    }

    private static void AssertPartitionsEqual(WindowPartitionSet expected, WindowPartitionSet actual)
    {
        Assert.AreEqual(expected.PartitionCount, actual.PartitionCount);
        for (var partitionIndex = 0; partitionIndex < expected.PartitionCount; partitionIndex++)
        {
            Assert.AreEqual(expected.GetLength(partitionIndex), actual.GetLength(partitionIndex));
            for (var index = 0; index < expected.GetLength(partitionIndex); index++)
            {
                Assert.AreEqual(
                    expected.GetIndex(partitionIndex, index),
                    actual.GetIndex(partitionIndex, index),
                    $"partition {partitionIndex}, index {index}");
            }
        }
    }
}
