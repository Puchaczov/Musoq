using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class StoredNumericFilterTests : GenericEntityTestBase
{
    private const string Source =
        "with samples as (select a.Id as Id, b.Value as Value, b.Bound as Bound from #schema.first() a inner join #schema.second() b on a.Id = b.Id) ";

    public sealed record Sample(int Id, double? Value, double? Bound);

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(63)]
    [DataRow(64)]
    [DataRow(65)]
    [DataRow(1024)]
    [DataRow(1025)]
    public void StoredComparisonPreservesUnknownNaNAndSignedZeroAcrossBatchBoundaries(int count)
    {
        double?[] values = [null, double.NaN, -0.0d, +0.0d, double.NegativeInfinity, double.PositiveInfinity, -3d, 7d];
        var rows = Enumerable.Range(0, count).Select(i => new Sample(i, values[i % 8], values[(i / 8) % 8])).ToArray();
        const string sql =
            Source
            + "select r.Id as Id, r.Value as Value from samples r where (r.Value <> r.Bound or r.Value is null) and (r.Id >= 0 and r.Bound is not null)";
        using var query = CreateAndRunVirtualMachine(sql, rows, rows.Reverse().ToArray());
        using var result = query.Run(TestContext.CancellationToken);
        var actual = result.ToArray();
        var expected = rows.Where(r => r.Bound.HasValue && (!r.Value.HasValue || r.Value.Value != r.Bound.Value))
            .ToArray();
        Assert.HasCount(expected.Length, actual);
        for (var index = 0; index < actual.Length; index++)
        {
            Assert.AreEqual(expected[index].Id, actual[index].GetValue<int>(0));
            var value = actual[index].GetValue<double?>(1);
            Assert.AreEqual(expected[index].Value.HasValue, value.HasValue);
            Assert.AreEqual(
                BitConverter.DoubleToInt64Bits(expected[index].Value.GetValueOrDefault()),
                BitConverter.DoubleToInt64Bits(value.GetValueOrDefault())
            );
        }
    }
}
