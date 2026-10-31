using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class WindowIntOrderBuilderTests : GenericEntityTestBase
{
    [TestMethod]
    public void IntegerRanking_WhenDescending_ShouldKeepTypedPartitionsAndPeers()
        => AssertRanking(["eng", "eng", "sales", "eng", "sales", null, null], [200, 200, 100, 150, 120, 10, 10], true,
            [1, 1, 2, 3, 1, 1, 1], [1, 1, 2, 2, 1, 1, 1]);

    [TestMethod]
    public void IntegerRanking_WhenAscending_ShouldKeepTypedPartitionsAndPeers()
        => AssertRanking(["eng", "eng", "sales", "eng", "sales", null, null], [200, 200, 100, 150, 120, 10, 10], false,
            [2, 2, 1, 1, 2, 1, 1], [2, 2, 1, 1, 2, 1, 1]);

    [TestMethod]
    public void IntegerRanking_WhenKeysRepeat_ShouldAssignRankAndDenseRankPeers()
        => AssertRanking(["eng", "eng", "eng", "eng", "eng"], [30, 20, 20, 10, 30], true,
            [1, 3, 3, 5, 1], [1, 2, 2, 3, 1]);

    [TestMethod]
    public void IntegerRanking_WhenPartitionsDiffer_ShouldRankIndependently()
        => AssertRanking(["eng", "sales", "eng", "sales", "eng"], [10, 10, 20, 5, 20], true,
            [3, 1, 1, 2, 1], [2, 1, 1, 2, 1]);

    [TestMethod]
    public void IntegerRanking_WhenPartitionIsNull_ShouldKeepItsIndependentPeers()
        => AssertRanking([null, "eng", null, "eng", null], [20, 10, 20, 30, 5], true,
            [1, 2, 1, 1, 3], [1, 2, 1, 1, 2]);

    [TestMethod]
    public void IntegerRanking_WhenQualified_ShouldRetainOnlyTheExpectedRanksAndPeers()
    {
        string?[] partitions = ["eng", "eng", "eng", "eng", "eng"];
        int[] keys = [30, 20, 20, 10, 30];
        AssertRanking(partitions, keys, true, [1, 3, 3, 5, 1], [1, 2, 2, 3, 1], "R <= 2", [0, 4]);
        AssertRanking(partitions, keys, true, [1, 3, 3, 5, 1], [1, 2, 2, 3, 1], "D <= 2", [0, 1, 2, 4]);
    }

    [TestMethod]
    public void PartitionedBuilder_WhenRequestedTwice_ShouldReuseSortedPartitionSet()
    {
        var builder = new WindowIntOrderBuilder<string>(3);
        builder.Add("eng", 20, 0);
        builder.Add("eng", 10, 1);
        builder.Add("sales", 30, 2);
        var first = builder.ToSortedPartitionSet(true);
        var second = builder.ToSortedPartitionSet(true);
        Assert.AreSame(first, second);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, first.Indices);
    }

    [TestMethod]
    public void IntegerRanking_WhenUnpartitioned_ShouldKeepSourceOrderAndPeerResults()
        => AssertRanking([null, null, null, null, null, null], [3, 8, 8, -1, 5, 3], true,
            [4, 1, 1, 6, 3, 4], [3, 1, 1, 4, 2, 3]);

    private void AssertRanking(string?[] partitions, int[] keys, bool descending, long[] ranks, long[] dense,
        string? qualify = null, int[]? positions = null)
    {
        var direction = descending ? "desc" : "asc";
        var suffix = qualify is null ? string.Empty : " qualify " + qualify;
        var sql = $"select Position, Rank() over (partition by Partition order by Key {direction}) as R, " +
            $"DenseRank() over (partition by Partition order by Key {direction}) as D, " +
            $"RowNumber() over (partition by Partition order by Key {direction}, Position) as N " +
            "from #schema.first()" + suffix + " order by Position";
        var input = keys.Select((key, index) => new RankingInput(index, partitions[index], key)).ToArray();
        using var query = CreateAndRunVirtualMachine(sql, input);
        using var table = query.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        positions ??= Enumerable.Range(0, input.Length).ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("R", typeof(long)),
            ("D", typeof(long)), ("N", typeof(long)));
        Assert.HasCount(positions.Length, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            var position = positions[index];
            var source = input[position];
            var earlier = input.LongCount(candidate => candidate.Partition == source.Partition &&
                (descending ? candidate.Key > source.Key : candidate.Key < source.Key));
            var tiedEarlier = input.LongCount(candidate => candidate.Partition == source.Partition &&
                candidate.Key == source.Key && candidate.Position < position);
            Assert.AreEqual(position, rows[index].GetValue<int>(0));
            Assert.AreEqual(ranks[position], rows[index].GetValue<long>(1));
            Assert.AreEqual(dense[position], rows[index].GetValue<long>(2));
            Assert.AreEqual(earlier + tiedEarlier + 1, rows[index].GetValue<long>(3));
        }
    }

    public sealed record RankingInput(int Position, string? Partition, int Key);
}
