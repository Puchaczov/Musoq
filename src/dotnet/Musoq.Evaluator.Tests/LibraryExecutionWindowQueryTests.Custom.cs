using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void GenericWindow_WhenArgumentsHaveDifferentConcreteTypes_ShouldCloseEachKernelIndependently()
    {
        const string query = """
            select Position, GenericTotal(Integral) over (partition by Partition order by Position) as Integers,
                GenericTotal(Amount) over (partition by Partition order by Position) as Decimals
            from #schema.first() order by Position
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)),
            ("Integers", typeof(int?)), ("Decimals", typeof(decimal?)));
        Assert.HasCount(5, rows);
        int?[] integers = [1, 3, 3, -3, 2];
        decimal?[] decimals = [1.25m, 4m, 4m, -3m, 2m];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(integers[index], rows[index].GetValue<int?>(1));
            Assert.AreEqual(decimals[index], rows[index].GetValue<decimal?>(2));
        }
    }

    [TestMethod]
    public void CustomKernels_WhenArgumentsAndResultsDiffer_ShouldKeepTypedStateAndFrameArguments()
    {
        const string query = """
            select Position,
                ScaledTotal(Amount, Position) over (partition by Partition order by Position
                    rows between 1 preceding and current row) as Scaled,
                SeededTotal(Amount) over (partition by Partition order by Position
                    rows between unbounded preceding and current row) as Seeded,
                PositiveCount(Amount) over (partition by Partition) as Positive
            from #schema.first() order by Position
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Scaled", typeof(decimal?)),
            ("Seeded", typeof(decimal)), ("Positive", typeof(long)));
        Assert.HasCount(5, rows);
        decimal?[] scaled = [1.25m, 6.75m, 5.5m, -12m, 13m];
        decimal[] seeded = [101.25m, 104m, 104m, 97m, 102m];
        long[] positive = [2L, 2L, 2L, 1L, 1L];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(scaled[index], rows[index].GetValue<decimal?>(1));
            Assert.AreEqual(seeded[index], rows[index].GetValue<decimal>(2));
            Assert.AreEqual(positive[index], rows[index].GetValue<long>(3));
        }
    }

    [TestMethod]
    public void CountStarWindow_WhenFramesContainNulls_ShouldCountRowsAsLong()
    {
        const string query = """
            select Position, Count(*) over (order by Position rows between 1 preceding and current row) as Rows
            from #schema.first() order by Position
            """;
        NumericRow<int>[] input = [new(1, null), new(2, null), new(3, 1)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Rows", typeof(long)));
        Assert.HasCount(3, rows);
        Assert.AreEqual(1L, rows[0].GetValue<long>(1));
        Assert.AreEqual(2L, rows[1].GetValue<long>(1));
        Assert.AreEqual(2L, rows[2].GetValue<long>(1));
    }
}
