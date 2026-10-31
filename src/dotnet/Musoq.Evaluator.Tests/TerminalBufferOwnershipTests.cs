using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TerminalBufferOwnershipTests
{
    private const string WindowQuery = """
        select Position, Partition, Sum(Amount) over (order by Position
            rows between current row and current row) as Total
        from #schema.first() order by Total desc, Partition asc
        """;

    [TestMethod]
    public void OwnedWindowBuffer_StableCompositeSort_PreservesTypedTableResultsAndInputAcrossRuns()
    {
        LibraryExecutionEntity[] input =
        [
            new(3, "A", null, null, null), new(1, "B", 2, 2m, null),
            new(2, "B", 2, 2m, null), new(4, "A", 1, 1m, null), new(5, "A", 2, 2m, null)
        ];
        var provider = LibraryExecutionFixture.CreateProvider(input);
        var logger = new TestsLoggerResolver();
        using var tableQuery = InstanceCreator.CompileForExecution(WindowQuery, Guid.NewGuid().ToString(), provider, logger);
        var typedQuery = InstanceCreator.CompileForTypedExecution<WindowRow>(WindowQuery, Guid.NewGuid().ToString(), provider, logger);
        WindowRow[] expected = [new(5, "A", 2m), new(1, "B", 2m), new(2, "B", 2m), new(4, "A", 1m), new(3, "A", null)];

        using var first = tableQuery.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(first,
            [5, "A", 2m], [1, "B", 2m], [2, "B", 2m], [4, "A", 1m], [3, "A", null]);
        var firstTyped = typedQuery.Run().ToArray();
        using var second = tableQuery.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(second,
            [5, "A", 2m], [1, "B", 2m], [2, "B", 2m], [4, "A", 1m], [3, "A", null]);
        CollectionAssert.AreEqual(expected, typedQuery.Run().ToArray());
        CollectionAssert.AreEqual(expected, firstTyped);
        TableMaterializationTestHelper.AssertRowsInOrder(first,
            [5, "A", 2m], [1, "B", 2m], [2, "B", 2m], [4, "A", 1m], [3, "A", null]);
        CollectionAssert.AreEqual(new[] { 3, 1, 2, 4, 5 }, input.Select(static row => row.Position).ToArray());
    }

    [TestMethod]
    public void TerminalRecordBuffer_PrivateSortKeys_DoNotBecomeOutputColumns()
    {
        const string query = "select Partition, Amount + 1 as Total from #schema.first() order by Position desc";
        var provider = LibraryExecutionFixture.CreateProvider();
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), provider, new TestsLoggerResolver());
        using var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Partition", typeof(string)), ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            ["B", 6m], ["B", -2m], ["A", null], ["A", 3.75m], ["A", 2.25m]);
    }

    public readonly record struct WindowRow(int Position, string Partition, decimal? Total);
}
