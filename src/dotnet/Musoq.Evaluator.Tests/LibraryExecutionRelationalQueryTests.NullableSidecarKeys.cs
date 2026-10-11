using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionNullableSidecarKeyQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void CteHashJoin_WhenCompositeKeysDifferOnlyInNullability_ShouldUseConcreteKeys()
    {
        const string query = """
            with Indexed as (select Position, Key, Tag from #schema.first())
            select a.Position as FirstPosition, b.Position as SecondPosition
            from #schema.first() a inner join Indexed b
                on a.NullableKey = b.Key and a.Tag = b.Tag
            order by FirstPosition, SecondPosition
            """;
        NullableKeyInput[] input = [new(1, 10, 10, "A"), new(2, 10, null, "A"),
            new(3, 10, 10, "B"), new(4, 99, 99, "A")];
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(input), LoggerResolver,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false, useCteSidecarIndexes: true));
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table,
            ("FirstPosition", typeof(int)), ("SecondPosition", typeof(int)));
        Assert.HasCount(4, rows);
        (int First, int Second)[] expected = [(1, 1), (1, 2), (3, 3), (4, 4)];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(expected[index].First, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index].Second, rows[index].GetValue<int>(1));
        }
    }

    [TestMethod]
    public void Window_WhenPartitionKeyIsNullableNumeric_ShouldPreserveTheNullGroup()
    {
        const string query = "select Position, Sum(Key) over (partition by NullableKey) as Total " +
            "from #schema.first() order by Position";
        NullableKeyInput[] input = [new(1, 10, 10, "A"), new(2, 5, null, "A"),
            new(3, 20, 10, "B"), new(4, 7, null, "B")];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(int?)));
        Assert.HasCount(4, rows);
        int?[] expected = [30, 12, 30, 12];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index], rows[index].GetValue<int?>(1));
        }
    }

    public sealed record NullableKeyInput(int Position, int Key, int? NullableKey, string Tag);
}
