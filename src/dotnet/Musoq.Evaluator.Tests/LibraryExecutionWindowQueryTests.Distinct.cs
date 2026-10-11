using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void DistinctWindows_WhenFramesRepeatValues_ShouldUseIndependentTypedSets()
    {
        const string query = """
            select Position,
                SumDistinct(Value) over (order by Position rows between 2 preceding and current row) as Total,
                AvgDistinct(Value) over (order by Position rows between 2 preceding and current row) as Average,
                CountDistinct(Value) over (order by Position rows between 2 preceding and current row) as Values
            from #schema.first() order by Position
            """;
        NumericRow<int>[] input = [new(1, 1), new(2, 1), new(3, 4), new(4, null), new(5, 4)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(int?)),
            ("Average", typeof(decimal?)), ("Values", typeof(long)));
        Assert.HasCount(5, rows);
        int?[] totals = [1, 1, 5, 5, 4];
        decimal?[] averages = [1m, 1m, 2.5m, 2.5m, 4m];
        long[] counts = [1L, 1L, 2L, 2L, 1L];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(totals[index], rows[index].GetValue<int?>(1));
            Assert.AreEqual(averages[index], rows[index].GetValue<decimal?>(2));
            Assert.AreEqual(counts[index], rows[index].GetValue<long>(3));
        }
    }
}
