using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionWideRangeKeyQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void RangeJoin_WithNinePartitionFields_ShouldKeepTypedKeysAndExcludeNullPartitions()
    {
        const string query = WideRangeQuery;
        var input = CreateRows();
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(input), LoggerResolver,
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: true));
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table,
            ("FirstPosition", typeof(int)), ("SecondPosition", typeof(int)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(2, rows[0].GetValue<int>(0));
        Assert.AreEqual(1, rows[0].GetValue<int>(1));
    }

    [TestMethod]
    public void Window_WithNinePartitionFields_ShouldKeepTypedKeysAndIncludeNullPartitions()
    {
        const string query = """
            select Position, Sum(Amount) over
                (partition by K1, K2, K3, K4, K5, K6, K7, K8, K9) as Total
            from #schema.first() order by Position
            """;
        var input = CreateRows().Append(new WideRangeInput(5, 50m, 1, 2, 3, 4, 5, 6, 7, 8, null)).ToArray();
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(5, rows);
        decimal?[] expected = [30m, 30m, 30m, 90m, 90m];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index], rows[index].GetValue<decimal?>(1));
        }
    }

    public const string WideRangeQuery = """
            select a.Position as FirstPosition, b.Position as SecondPosition
            from #schema.first() a inner join #schema.first() b
                on a.K1 = b.K1 and a.K2 = b.K2 and a.K3 = b.K3
                and a.K4 = b.K4 and a.K5 = b.K5 and a.K6 = b.K6
                and a.K7 = b.K7 and a.K8 = b.K8 and a.K9 = b.K9
                and a.Amount > b.Amount
            order by FirstPosition, SecondPosition
            """;

    public static WideRangeInput[] CreateRows() =>
        [
            new(1, 10m, 1, 2, 3, 4, 5, 6, 7, 8, "A"),
            new(2, 20m, 1, 2, 3, 4, 5, 6, 7, 8, "A"),
            new(3, 30m, 1, 2, 3, 4, 5, 6, 7, 9, "A"),
            new(4, 40m, 1, 2, 3, 4, 5, 6, 7, 8, null)
        ];

    public sealed record WideRangeInput(int Position, decimal Amount,
        int K1, int K2, int K3, int K4, int K5, int K6, int K7, int K8, string? K9);
}
