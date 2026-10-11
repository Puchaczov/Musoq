using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void EmptyRowsFrames_ShouldReturnNullAggregatesAndZeroCount()
    {
        const string query = """
            select Position,
                Sum(Value) over (order by Position rows between 1 following and 1 following) as Total,
                Avg(Value) over (order by Position rows between 1 following and 1 following) as Average,
                Count(Value) over (order by Position rows between 1 following and 1 following) as Values
            from #schema.first() order by Position
            """;
        NumericRow<int>[] input = [new(1, 2), new(2, null)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(int?)),
            ("Average", typeof(decimal?)), ("Values", typeof(long)));
        Assert.HasCount(2, rows);
        Assert.IsNull(rows[0].GetValue<int?>(1));
        Assert.IsNull(rows[0].GetValue<decimal?>(2));
        Assert.AreEqual(0L, rows[0].GetValue<long>(3));
        Assert.IsNull(rows[1].GetValue<int?>(1));
        Assert.IsNull(rows[1].GetValue<decimal?>(2));
        Assert.AreEqual(0L, rows[1].GetValue<long>(3));
    }

    [TestMethod]
    public void RangePeerFrames_ShouldIncludeAllPeersWithConcreteResults()
    {
        const string query = """
            select Position, Sum(Value) over (order by Value range between unbounded preceding and current row) as Total,
                Avg(Value) over (order by Value range between unbounded preceding and current row) as Average
            from #schema.first() order by Position
            """;
        NumericRow<int>[] input = [new(1, 1), new(2, 2), new(3, 2)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(int?)),
            ("Average", typeof(decimal?)));
        Assert.HasCount(3, rows);
        Assert.AreEqual(1, rows[0].GetValue<int?>(1));
        Assert.AreEqual(1m, rows[0].GetValue<decimal?>(2));
        Assert.AreEqual(5, rows[1].GetValue<int?>(1));
        Assert.AreEqual(5m / 3m, rows[1].GetValue<decimal?>(2));
        Assert.AreEqual(5, rows[2].GetValue<int?>(1));
        Assert.AreEqual(5m / 3m, rows[2].GetValue<decimal?>(2));
    }
}
