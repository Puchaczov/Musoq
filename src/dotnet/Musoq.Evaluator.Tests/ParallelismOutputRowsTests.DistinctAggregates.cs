using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class ParallelismOutputRowsTests
{
    [TestMethod]
    public void GroupBy_CountDistinct_LargeInputWithNulls_ShouldMatchSerialAndIndependentOracle()
    {
        const int rowCount = 12_288;
        const string nullKey = "<null-key>";
        const string query = "select City, Count(distinct Name) as DistinctNames from #A.Entities() group by City";
        var entities = Enumerable.Range(0, rowCount)
            .Select(index => new BasicEntity
            {
                City = index % 19 == 0 ? null : $"City_{index % 7}",
                Name = index % 23 == 0 ? null : $"Name_{index % 29}",
                Id = index,
                Population = index % 101
            })
            .ToList();

        var expected = entities
            .GroupBy(entity => entity.City)
            .ToDictionary(
                group => group.Key ?? nullKey,
                group => group
                    .Select(entity => entity.Name)
                    .Where(static value => value is not null)
                    .Distinct()
                    .LongCount());

        var parallelTable = CreateVirtualMachineWithOptions(
                query,
                new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", entities.ToList() } },
                new CompilationOptions(ParallelizationMode.Full, maxDegreeOfParallelismOverride: 2))
            .Run();
        var serialTable = CreateVirtualMachineWithOptions(
                query,
                new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", entities.ToList() } },
                new CompilationOptions(ParallelizationMode.None, maxDegreeOfParallelismOverride: 2))
            .Run();

        AssertDistinctCounts(expected, parallelTable, nullKey);
        AssertDistinctCounts(expected, serialTable, nullKey);
    }

    private static void AssertDistinctCounts(
        IReadOnlyDictionary<string, long> expected,
        Tables.Table table,
        string nullKey)
    {
        var actual = table.ToDictionary(
            row => (string?)row[0] ?? nullKey,
            row => Convert.ToInt64(row[1]));

        Assert.AreEqual(expected.Count, actual.Count);
        foreach (var pair in expected)
        {
            Assert.IsTrue(actual.TryGetValue(pair.Key, out var value), $"Missing group {pair.Key}.");
            Assert.AreEqual(pair.Value, value, $"Unexpected distinct count for group {pair.Key}.");
        }
    }
}
