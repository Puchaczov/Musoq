using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionCoverageQueryTests
{
    public const string SetAggregateWindowQuery = """
        with doubled as (
            select Partition, Value from #schema.first()
            union all select Partition, Value from #schema.first() where Position % 2 = 0),
        totals as (
            select Partition, Sum(Value) as Total, Count(Value) as Present,
                Sum(distinct Value) as UniqueTotal from doubled group by Partition)
        select Partition, Total, Present, UniqueTotal, Avg(Total) over () as AverageTotal
        from totals
        """;

    [TestMethod]
    public void SetGroupedAndWindowPipeline_WhenRepeated_ShouldMatchIndependentMultisetReference()
    {
        var input = CreateRows(4099);
        var groups = input.Concat(input.Where(row => row.Position % 2 == 0))
            .GroupBy(row => row.Partition).ToArray();
        var expected = groups.Select(group =>
        {
            var values = group.Where(row => row.Value.HasValue).Select(row => row.Value!.Value).ToArray();
            int? total = values.Length == 0 ? null : values.Sum();
            int? distinctTotal = values.Length == 0 ? null : values.Distinct().Sum();
            return (Partition: group.Key, Total: total, Count: (long)values.Length, UniqueTotal: distinctTotal);
        }).ToArray();
        var average = expected.Where(row => row.Total.HasValue).Average(row => (decimal)row.Total!.Value);
        using var compiled = CreateAndRunVirtualMachine(SetAggregateWindowQuery, input);
        for (var execution = 0; execution < 2; execution++)
        {
            using var table = compiled.Run(TestContext.CancellationToken);
            TableMaterializationTestHelper.AssertColumns(table, ("Partition", typeof(int?)), ("Total", typeof(int?)),
                ("Present", typeof(long)), ("UniqueTotal", typeof(int?)), ("AverageTotal", typeof(decimal?)));
            var rows = table.ToArray();
            Assert.HasCount(expected.Length, rows);
            foreach (var reference in expected)
            {
                var actual = rows.Single(row => row.GetValue<int?>(0) == reference.Partition);
                Assert.AreEqual(reference.Total, actual.GetValue<int?>(1));
                Assert.AreEqual(reference.Count, actual.GetValue<long>(2));
                Assert.AreEqual(reference.UniqueTotal, actual.GetValue<int?>(3));
                Assert.AreEqual(average, actual.GetValue<decimal?>(4));
            }
        }
    }
}
