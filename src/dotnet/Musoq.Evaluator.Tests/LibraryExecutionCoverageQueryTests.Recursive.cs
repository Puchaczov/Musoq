using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionCoverageQueryTests
{
    public const string RecursiveFrameQuery = """
        with recursive counter (Value) as (
            select 0 from values {(Seed: 1)} seed union all
            select c.Value + 1 from counter c where c.Value < 39)
        select Value, Sum(Value) over (order by Value rows between 2 preceding and current row) as Total,
            Avg(Value) over () as Average,
            Count(Value) over (partition by Value % 3) as Present
        from counter order by Value
        """;

    [TestMethod]
    public void RecursiveTypedStorageAndFrames_ShouldMatchIndependentArithmeticReference()
    {
        using var compiled = CreateAndRunVirtualMachine(RecursiveFrameQuery, new[] { new CoverageRow(0, null, null) });
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int)), ("Total", typeof(int?)),
            ("Average", typeof(decimal?)), ("Present", typeof(long)));
        Assert.HasCount(40, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            var first = System.Math.Max(0, index - 2);
            var total = Enumerable.Range(first, index - first + 1).Sum();
            var count = Enumerable.Range(0, 40).LongCount(value => value % 3 == index % 3);
            Assert.AreEqual(index, rows[index].GetValue<int>(0));
            Assert.AreEqual(total, rows[index].GetValue<int?>(1));
            Assert.AreEqual(19.5m, rows[index].GetValue<decimal?>(2));
            Assert.AreEqual(count, rows[index].GetValue<long>(3));
        }
    }
}
