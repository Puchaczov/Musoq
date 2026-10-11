using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void ProfiledTypedWindowResult_ShouldConsumeConcreteNullableValues(QueryInstrumentationMode mode)
    {
        const string query = """
            select Position, Sum(Amount) over (partition by Partition order by Position
                rows between 1 preceding and current row) as Total
            from #schema.first() order by Position
            """;
        var compiled = InstanceCreator.CompileForTypedProfile<WindowOutput>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver, new CompilationOptions(instrumentationMode: mode));
        var result = compiled.RunWithProfile(TestContext.CancellationToken);
        var rows = result.Rows.ToArray();

        WindowOutput[] expected = [new(1, 1.25m), new(2, 4m), new(3, 2.75m), new(4, -3m), new(5, 2m)];
        CollectionAssert.AreEqual(expected, rows);
    }

    [TestMethod]
    public void TypedWindowResult_WhenFilteredSortedAndPaged_ShouldPreserveNullableValues()
    {
        const string query = """
            select Position, Sum(Amount) over (partition by Partition order by Position
                rows between 1 preceding and current row) as Total
            from #schema.first() qualify RowNumber() over (order by Position) > 1
            order by Position desc skip 1 take 3
            """;
        var compiled = InstanceCreator.CompileForTypedExecution<WindowOutput>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        var rows = compiled.Run(TestContext.CancellationToken).ToArray();

        Assert.HasCount(3, rows);
        Assert.AreEqual(new WindowOutput(4, -3m), rows[0]);
        Assert.AreEqual(new WindowOutput(3, 2.75m), rows[1]);
        Assert.AreEqual(new WindowOutput(2, 4m), rows[2]);
    }

    [TestMethod]
    public void TypedWindowResult_WhenExecutedRepeatedly_ShouldOwnStateAndStoragePerRun()
    {
        const string query = """
            select Position, SeededTotal(Amount) over (partition by Partition order by Position
                rows between unbounded preceding and current row) as Total
            from #schema.first() order by Position
            """;
        var compiled = InstanceCreator.CompileForTypedExecution<SeededWindowOutput>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        SeededWindowOutput[] expected = [new(1, 101.25m), new(2, 104m), new(3, 104m), new(4, 97m), new(5, 102m)];
        for (var iteration = 0; iteration < 5; iteration++)
        {
            var rows = compiled.Run(TestContext.CancellationToken).ToArray();
            CollectionAssert.AreEqual(expected, rows);
        }
    }

    public readonly record struct WindowOutput(int Position, decimal? Total);
    public readonly record struct SeededWindowOutput(int Position, decimal Total);
}
