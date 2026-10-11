using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Plugins;
using Musoq.Parser.Diagnostics;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class WindowPartitionSetTests : GenericEntityTestBase
{
    [TestMethod]
    public void TypedPartitions_ShouldKeepSeparateNullAndNamedGroups()
        => AssertOrdinals("partition by Partition order by Position",
            CreateOrdinalRows(["b", "a", "b", null, "a", null, "c"], [0, 1, 2, 3, 4, 5, 6]), [1, 1, 2, 1, 2, 2, 1]);

    [TestMethod]
    public void ErasedPartitionKeys_ShouldFailTheSourceContractInsteadOfCreatingObjectGroups()
    {
        const string sql = "select RowNumber() over (partition by Partition order by Position) as N from #schema.first()";
        var error = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(sql,
            new[] { new ErasedPartitionRow(0, "b"), new ErasedPartitionRow(1, 1) }));
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code, error.ToString());
        Assert.Contains("Partition", error.Message);
    }

    [TestMethod]
    public void ValuePartitions_ShouldUseConcreteStructEquality()
    {
        var keys = new[] { 2, 1, 2, 3, 1 };
        var input = keys.Select((key, index) => new StructPartitionRow(index, new DepartmentKey(key))).ToArray();
        AssertOrdinals("partition by Partition order by Position", input, [1, 1, 2, 1, 2]);
    }

    [TestMethod]
    public void UnpartitionedRows_ShouldKeepRequestedSequentialOrder()
        => AssertOrdinals("order by Position", CreateOrdinalRows([null, null, null, null, null], [0, 1, 2, 3, 4]), [1, 2, 3, 4, 5]);

    [TestMethod]
    public void TypedOrderKeys_ShouldKeepDescendingOrderInsideEachPartition()
        => AssertOrdinals("partition by Partition order by Key desc, Position",
            CreateOrdinalRows(["b", "a", "b", null, "a", null, "c"], [20, 10, 30, 5, 10, 7, 1]), [2, 1, 1, 2, 2, 1, 1]);

    [TestMethod]
    public void CompositeOrderKeys_ShouldHonorEachConcreteDirection()
    {
        CompositeOrderRow[] input = [new(0, "x", "b", 2), new(1, "x", "a", 3), new(2, "x", "a", 1),
            new(3, "y", "c", 1), new(4, "y", "b", 5)];
        AssertOrdinals("partition by Partition order by Label, Key desc", input, [3, 1, 2, 2, 1]);
    }

    [TestMethod]
    public void StructOrderKeys_ShouldSortThroughConcreteComparisons()
    {
        string[] partitions = ["a", "a", "a", "b", "b"];
        int[] keys = [20, 10, 30, 2, 1];
        var input = keys.Select((key, index) => new StructOrderRow(index, partitions[index], new ScoreKey(key))).ToArray();
        AssertOrdinals("partition by Partition order by Key", input, [2, 1, 3, 2, 1]);
        AssertOrdinals("partition by Partition order by Key desc", input, [2, 3, 1, 1, 2]);
    }

    [TestMethod]
    public void StructOrderKeys_ShouldKeepTypedRankPeerEquality()
    {
        var input = new[] { 20, 10, 20, 5 }.Select((key, index) => new StructOrderRow(index, "a", new ScoreKey(key))).ToArray();
        AssertRanks("partition by Partition order by Key desc", input, [1, 3, 1, 4], [1, 2, 1, 3]);
    }

    [TestMethod]
    public void ReferenceOrderKeys_ShouldHonorExplicitNullOrdering()
    {
        ReferenceOrderRow[] input = [new(0, "b"), new(1, null), new(2, "a"), new(3, null)];
        AssertOrdinals("order by Key nulls first, Position", input, [4, 1, 3, 2]);
        AssertOrdinals("order by Key desc nulls last, Position", input, [1, 3, 2, 4]);
    }

    [TestMethod]
    public void TypedRankPartitions_ShouldPreserveExpectedNamedAndNullPeers()
        => AssertRanks("partition by Partition order by Key desc",
            CreateOrdinalRows(["eng", "eng", "sales", "eng", "sales", null, null], [200, 200, 100, 150, 120, 10, 10]),
            [1, 1, 2, 3, 1, 1, 1], [1, 1, 2, 2, 1, 1, 1]);

    [TestMethod]
    public void OffsetPartitions_ShouldBindConcretePerRowOffsetsAndDefaults()
    {
        string?[] partitions = ["eng", "eng", "sales", "eng", "sales", null, null];
        int[] keys = [200, 200, 100, 150, 120, 10, 10];
        int[] offsets = [1, 2, 1, 1, 2, 1, 2];
        var input = keys.Select((key, index) => new OffsetRow(index, partitions[index], key, index + 1, offsets[index], -10 - index)).ToArray();
        const string sql = "select Position, Lag(Value, Offset, Fallback) over w as Previous, " +
            "Lead(Value, Offset, Fallback) over w as Next from #schema.first() " +
            "window w as (partition by Partition order by Key desc, Position) order by Position";
        using var compiled = CreateAndRunVirtualMachine(sql, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Previous", typeof(int?)), ("Next", typeof(int?)));
        Assert.HasCount(7, rows);
        CollectionAssert.AreEqual(new int?[] { -10, -11, 5, 2, -14, -15, -16 }, rows.Select(row => row.GetValue<int?>(1)).ToArray());
        CollectionAssert.AreEqual(new int?[] { 2, -11, -12, -13, -14, 7, -16 }, rows.Select(row => row.GetValue<int?>(2)).ToArray());
    }

    [TestMethod]
    public void TypedNumericWindows_WhenPartitionKeysAreNullable_ShouldPreserveAllAggregateResults()
    {
        const string query = "select Position, Sum(Value) over w as Total, Count(Value) over w as Present, Avg(Value) over w as Average, Min(Value) over w as Minimum, Max(Value) over w as Maximum from #schema.first() window w as (partition by Partition order by OrderKey, Position rows between unbounded preceding and current row) order by Position";
        NullablePartitionRow[] input = [new(0, "eng", 200, 10m), new(1, "eng", 200, null), new(2, "sales", 100, 30m), new(3, "eng", 150, 40m), new(4, "sales", 120, 50m), new(5, null, 10, 60m), new(6, null, 10, 70m)];
        using var compiled = CreateAndRunVirtualMachine<NullablePartitionRow, LibraryBase>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(decimal?)), ("Present", typeof(long)), ("Average", typeof(decimal?)), ("Minimum", typeof(decimal?)), ("Maximum", typeof(decimal?)));
        Assert.HasCount(7, rows);
        CollectionAssert.AreEqual(new decimal?[] { 50m, 50m, 30m, 40m, 80m, 60m, 130m }, rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
        CollectionAssert.AreEqual(new long[] { 2, 2, 1, 1, 2, 1, 2 }, rows.Select(row => row.GetValue<long>(2)).ToArray());
        CollectionAssert.AreEqual(new decimal?[] { 25m, 25m, 30m, 40m, 40m, 60m, 65m }, rows.Select(row => row.GetValue<decimal?>(3)).ToArray());
        CollectionAssert.AreEqual(new decimal?[] { 10m, 10m, 30m, 40m, 30m, 60m, 60m }, rows.Select(row => row.GetValue<decimal?>(4)).ToArray());
        CollectionAssert.AreEqual(new decimal?[] { 40m, 40m, 30m, 40m, 50m, 60m, 70m }, rows.Select(row => row.GetValue<decimal?>(5)).ToArray());
    }

    public sealed record NullablePartitionRow(int Position, string? Partition, int OrderKey, decimal? Value);

    [TestMethod]
    public void TypedLibraryWindows_WhenPartitionsAreOrdered_ShouldPreserveRunningAndWholePartitionResults()
    {
        const string query = "select Position, Sum(Value) over (partition by Partition order by OrderKey rows between unbounded preceding and current row) as Running, Sum(Value) over (partition by Partition) as Whole from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<PartitionRow, LibraryBase>(query, CreatePartitionRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Running", typeof(decimal?)), ("Whole", typeof(decimal?)));
        Assert.HasCount(5, rows);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new decimal?[] { 10m, 70m, 80m, 50m, 50m }, rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
        CollectionAssert.AreEqual(new decimal?[] { 70m, 70m, 80m, 70m, 80m }, rows.Select(row => row.GetValue<decimal?>(2)).ToArray());
    }

    [TestMethod]
    public void TypedLibraryWindows_WhenFramesAreBounded_ShouldPreservePartitionAndOrderingResults()
    {
        const string query = "select Position, Sum(Value) over (partition by Partition order by OrderKey rows between 1 preceding and current row) as Total from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<PartitionRow, LibraryBase>(query, CreatePartitionRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(5, rows);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new decimal?[] { 10m, 60m, 80m, 50m, 50m }, rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
    }

    public sealed record PartitionRow(int Position, string Partition, int OrderKey, decimal? Value);
    private static PartitionRow[] CreatePartitionRows() =>
        [new(0, "a", 1, 10m), new(1, "a", 3, 20m), new(2, "b", 2, 30m), new(3, "a", 2, 40m), new(4, "b", 1, 50m)];

    private void AssertOrdinals<T>(string specification, T[] input, long[] expected)
    {
        var sql = $"select Position, RowNumber() over ({specification}) as N from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine(sql, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("N", typeof(long)));
        Assert.HasCount(expected.Length, rows);
        CollectionAssert.AreEqual(Enumerable.Range(0, expected.Length).ToArray(), rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(expected, rows.Select(row => row.GetValue<long>(1)).ToArray());
    }

    private void AssertRanks<T>(string specification, T[] input, long[] ranks, long[] dense)
    {
        var sql = $"select Position, Rank() over ({specification}) as R, DenseRank() over ({specification}) as D " +
            "from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine(sql, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("R", typeof(long)), ("D", typeof(long)));
        Assert.HasCount(ranks.Length, rows);
        CollectionAssert.AreEqual(Enumerable.Range(0, ranks.Length).ToArray(), rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(ranks, rows.Select(row => row.GetValue<long>(1)).ToArray());
        CollectionAssert.AreEqual(dense, rows.Select(row => row.GetValue<long>(2)).ToArray());
    }

    private static OrdinalRow[] CreateOrdinalRows(string?[] partitions, int[] keys)
        => keys.Select((key, index) => new OrdinalRow(index, partitions[index], key)).ToArray();
    public sealed record OrdinalRow(int Position, string? Partition, int Key);
    public sealed record ErasedPartitionRow(int Position, object Partition);
    public sealed record StructPartitionRow(int Position, DepartmentKey Partition);
    public sealed record StructOrderRow(int Position, string Partition, ScoreKey Key);
    public sealed record CompositeOrderRow(int Position, string Partition, string Label, int Key);
    public sealed record ReferenceOrderRow(int Position, string? Key);
    public sealed record OffsetRow(int Position, string? Partition, int Key, int? Value, int Offset, int? Fallback);
    public readonly record struct DepartmentKey(int Value);
    public readonly record struct ScoreKey(int Value) : IComparable<ScoreKey>
    {
        public int CompareTo(ScoreKey other) => Value.CompareTo(other.Value);
    }
}
