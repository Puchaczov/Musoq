using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

using static Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTestsFixtures;

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
}
