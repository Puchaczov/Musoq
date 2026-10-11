using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class LibraryExecutionCoverageQueryTests : GenericEntityTestBase
{
    public const string JoinedFrameQuery = """
        with joined as (
            select a.Position as Position, a.Partition as Partition,
                a.Value + b.Value as Value
            from #schema.first() a left join #schema.first() b on b.Position = a.Position - 1)
        select Position, Partition,
            Sum(Value) over (partition by Partition order by Position
                rows between 2 preceding and 1 following) as Total,
            Avg(Value) over (partition by Partition order by Position
                rows between 2 preceding and 1 following) as Average,
            Count(Value) over (partition by Partition order by Position
                rows between 2 preceding and 1 following) as Present,
            Lag(Value, 1) over (partition by Partition order by Position) as Previous,
            RowNumber() over (partition by Partition order by Position) as Number
        from joined qualify Number <= 5 order by Position
        """;

    [TestMethod]
    [DataRow(17)]
    [DataRow(7919)]
    public void JoinedNullableFramesAndQualify_ShouldMatchIndependentSeededReference(int seed)
    {
        var input = CreateRows(seed);
        using var compiled = CreateAndRunVirtualMachine(JoinedFrameQuery, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var actual = table.ToArray();
        var expected = CalculateJoinedFrames(input);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Partition", typeof(int?)),
            ("Total", typeof(int?)), ("Average", typeof(decimal?)), ("Present", typeof(long)),
            ("Previous", typeof(int?)), ("Number", typeof(long)));
        Assert.HasCount(expected.Length, actual);
        for (var index = 0; index < expected.Length; index++)
        {
            var row = actual[index];
            var reference = expected[index];
            Assert.AreEqual(reference.Position, row.GetValue<int>(0));
            Assert.AreEqual(reference.Partition, row.GetValue<int?>(1));
            Assert.AreEqual(reference.Total, row.GetValue<int?>(2));
            Assert.AreEqual(reference.Average, row.GetValue<decimal?>(3));
            Assert.AreEqual(reference.Present, row.GetValue<long>(4));
            Assert.AreEqual(reference.Previous, row.GetValue<int?>(5));
            Assert.AreEqual(reference.Number, row.GetValue<long>(6));
        }
    }

    public static CoverageRow[] CreateRows(int seed)
    {
        var random = new Random(seed);
        return Enumerable.Range(0, 180).Select(position => new CoverageRow(position,
            position % 9 == 8 ? null : position % 9,
            position % 9 == 7 || random.Next(5) == 0 ? null : random.Next(-100, 101))).ToArray();
    }

    private static FrameReference[] CalculateJoinedFrames(CoverageRow[] input)
    {
        var joined = input.Select(row => row with
        {
            Value = row.Position == 0 ? null : row.Value + input[row.Position - 1].Value
        });
        return joined.GroupBy(row => row.Partition).SelectMany(partition =>
        {
            var ordered = partition.OrderBy(row => row.Position).ToArray();
            return Enumerable.Range(0, Math.Min(5, ordered.Length)).Select(index => CalculateFrame(ordered, index));
        }).OrderBy(row => row.Position).ToArray();
    }

    private static FrameReference CalculateFrame(CoverageRow[] ordered, int index)
    {
        int? total = null;
        long count = 0;
        for (var frame = Math.Max(0, index - 2); frame <= Math.Min(ordered.Length - 1, index + 1); frame++)
        {
            if (ordered[frame].Value is not { } value)
                continue;
            total = checked(total.GetValueOrDefault() + value);
            count++;
        }
        decimal? average = count == 0 ? null : (decimal)total!.Value / count;
        return new FrameReference(ordered[index].Position, ordered[index].Partition, total, average, count,
            index == 0 ? null : ordered[index - 1].Value, index + 1L);
    }

    public sealed record CoverageRow(int Position, int? Partition, int? Value);
    private sealed record FrameReference(int Position, int? Partition, int? Total, decimal? Average,
        long Present, int? Previous, long Number);
}
