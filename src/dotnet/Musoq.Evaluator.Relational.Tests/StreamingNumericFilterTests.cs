using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class StreamingNumericFilterTests : GenericEntityTestBase
{
    public sealed class Observation(int id, int? reading, bool upperBoundAvailable)
    {
        public int Id => id;
        public int? Reading => reading;
        public int? Low => 0;
        public int? High => upperBoundAvailable ? 10 : throw new InvalidOperationException("Upper bound was read.");
    }

    [TestMethod]
    public void TakeOneStopsBeforeTheNextMatchingRowInTheSameChunk()
    {
        const string sql =
            "select r.Id, r.Reading * r.Reading from #schema.first() r where r.Reading between r.Low and r.High take 1";
        Observation[] input = [new(0, 3, true), new(1, 4, false)];
        using var query = CreateAndRunVirtualMachine(sql, input);
        using var result = query.Run(TestContext.CancellationToken);
        var row = result.First();
        Assert.AreEqual(0, row.GetValue<int>(0));
        Assert.AreEqual(9, row.GetValue<int?>(1));
    }

    [TestMethod]
    public void EagerSortSkipsConditionalUpperBoundWhenLowerComparisonIsFalse()
    {
        const string sql =
            "select r.Id, r.Reading * r.Reading from #schema.first() r where (r.Reading between r.Low and r.High or r.Reading is null) and r.Id >= 0 order by r.Id";
        Observation[] input = [new(3, -1, false), new(2, 4, true), new(1, null, true)];
        using var query = CreateAndRunVirtualMachine(sql, input);
        using var result = query.Run(TestContext.CancellationToken);
        var rows = result.ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        Assert.IsNull(rows[0].GetValue<int?>(1));
        Assert.AreEqual(16, rows[1].GetValue<int?>(1));
    }
}
