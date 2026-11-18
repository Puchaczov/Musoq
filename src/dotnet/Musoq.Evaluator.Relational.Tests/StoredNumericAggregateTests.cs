using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class StoredNumericAggregateTests : GenericEntityTestBase
{
    private const string Source =
        "with samples as (select a.Id as Id, b.Bucket as Bucket, b.Reading as Reading, b.Score as Score from #schema.first() a inner join #schema.second() b on a.Id = b.Id) ";

    public sealed record Sample(int Id, int Bucket, long? Reading, long Score);

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(65)]
    [DataRow(1025)]
    public void GlobalReductionsPreserveEmptyResultsTailsAndBothIntegerExtremes(int count)
    {
        var input = MakeRows(count);
        const string sql =
            Source
            + "select Count(*) filter (where r.Score >= 0) as Positive, Min(r.Score) as Minimum, Max(r.Score) as Maximum from samples r";
        using var query = CreateAndRunVirtualMachine(sql, input, input.Reverse().ToArray());
        using var table = query.Run(TestContext.CancellationToken);
        var row = table.Single();
        Assert.AreEqual(input.LongCount(r => r.Score >= 0), row.GetValue<long>(0));
        Assert.AreEqual(input.Select(r => (long?)r.Score).Min(), row.GetValue<long?>(1));
        Assert.AreEqual(input.Select(r => (long?)r.Score).Max(), row.GetValue<long?>(2));
    }

    [TestMethod]
    public void GroupedReductionsKeepAllNullAndZeroCountGroupsInFirstSeenOrder()
    {
        var input = MakeRows(1025);
        const string sql =
            Source
            + "select r.Bucket, Count(*) filter (where r.Score > 0 and r.Bucket <> 3) as Positive, Min(r.Reading) as Minimum, Max(r.Reading) as Maximum from samples r group by r.Bucket";
        using var query = CreateAndRunVirtualMachine(sql, input, input.Reverse().ToArray());
        using var table = query.Run(TestContext.CancellationToken);
        var actual = table.ToArray();
        var groups = input.GroupBy(r => r.Bucket).ToArray();
        Assert.HasCount(groups.Length, actual);
        for (int index = 0; index < groups.Length; index++)
        {
            var expected = groups[index];
            Assert.AreEqual(expected.Key, actual[index].GetValue<int>(0));
            Assert.AreEqual(expected.LongCount(r => r.Score > 0 && r.Bucket != 3), actual[index].GetValue<long>(1));
            Assert.AreEqual(expected.Min(r => r.Reading), actual[index].GetValue<long?>(2));
            Assert.AreEqual(expected.Max(r => r.Reading), actual[index].GetValue<long?>(3));
        }
    }

    private static Sample[] MakeRows(int count) =>
        Enumerable
            .Range(0, count)
            .Select(i =>
            {
                var bucket = (i + 3) % 5;
                var score =
                    i == 0 ? long.MinValue
                    : i == count - 1 ? long.MaxValue
                    : i - 31L;
                return new Sample(i, bucket, bucket == 3 || i % 7 == 0 ? null : score, score);
            })
            .ToArray();
}
