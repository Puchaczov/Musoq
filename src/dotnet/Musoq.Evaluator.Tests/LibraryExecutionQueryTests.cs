using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void GroupedSum_WhenInputTypesDiffer_ShouldPreserveEachConcreteResultType()
    {
        const string query = """
            select Partition, Sum(Integral) as Integers, Sum(Amount) as Decimals,
                   Sum(Floating) as Doubles, Count(Amount) as Values
            from #schema.first()
            group by Partition
            order by Partition
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result,
            ("Partition", typeof(string)), ("Integers", typeof(int?)),
            ("Decimals", typeof(decimal?)), ("Doubles", typeof(double?)), ("Values", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            ["A", 3, 4m, 4d, 2L], ["B", 2, 2m, 2d, 2L]);
    }

    [TestMethod]
    public void CustomAggregates_WhenCombinedWithInheritedSum_ShouldKeepIndependentTypedState()
    {
        const string query = """
            select Partition, PositiveTotal(Amount) as Positive,
                   ScaledTotal(Amount, 2) as Scaled, Sum(Amount) as Total
            from #schema.first()
            group by Partition
            order by Partition
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result,
            ("Partition", typeof(string)), ("Positive", typeof(decimal?)),
            ("Scaled", typeof(decimal?)), ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            ["A", 4m, 8m, 4m], ["B", 5m, 4m, 2m]);
    }

    [TestMethod]
    public void Aggregates_WhenEveryInputIsNull_ShouldReturnNullTotalsAndZeroCount()
    {
        const string query = """
            select Sum(Amount) as Total, PositiveTotal(Amount) as Positive,
                   ScaledTotal(Amount, 2) as Scaled, Count(Amount) as Values
            from #schema.first()
            """;
        LibraryExecutionEntity[] rows = [new(1, "A", null, null, null)];
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query, rows);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result,
            ("Total", typeof(decimal?)), ("Positive", typeof(decimal?)),
            ("Scaled", typeof(decimal?)), ("Values", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, [null, null, null, 0L]);
    }

    [TestMethod]
    public void CustomAggregate_WhenQueryRunsTwice_ShouldResetEveryGroupState()
    {
        const string query = "select PositiveTotal(Amount) as Total from #schema.first()";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());

        TableMaterializationTestHelper.AssertRowsInOrder(compiled.Run(TestContext.CancellationToken), [9m]);
        TableMaterializationTestHelper.AssertRowsInOrder(compiled.Run(TestContext.CancellationToken), [9m]);
    }

    [TestMethod]
    public void RunningWindow_WhenPartitionsChange_ShouldResetSumAndRowNumber()
    {
        const string query = """
            select Position, Sum(Amount) over (
                partition by Partition order by Position
                rows between unbounded preceding and current row) as Total,
                RowNumber() over (partition by Partition order by Position) as Number
            from #schema.first()
            order by Position
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);

        _ = result.Count;
        TableMaterializationTestHelper.AssertColumns(result,
            ("Position", typeof(int)), ("Total", typeof(decimal?)), ("Number", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            [1, 1.25m, 1L], [2, 4m, 2L], [3, 4m, 3L], [4, -3m, 1L], [5, 2m, 2L]);
    }

    [TestMethod]
    public void CompositeGroups_WhenOneKeyIsNull_ShouldKeepEachGroupSeparate()
    {
        const string query = """
            select Partition, Integral, Sum(Amount) as Total, Count(*) as Rows
            from #schema.first() group by Partition, Integral
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result,
            ("Partition", typeof(string)), ("Integral", typeof(int?)),
            ("Total", typeof(decimal?)), ("Rows", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            ["A", 1, 1.25m, 1L], ["A", 2, 2.75m, 1L], ["A", null, null, 1L],
            ["B", -3, -3m, 1L], ["B", 5, 5m, 1L]);
    }

    [TestMethod]
    public void GroupedCte_WhenConsumedByWindow_ShouldPreserveGroupTotals()
    {
        const string query = """
            with totals as (
                select Partition, Sum(Amount) as Total from #schema.first() group by Partition
            )
            select Partition, Total, Sum(Total) over (order by Partition) as RunningTotal
            from totals order by Partition
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result,
            ("Partition", typeof(string)), ("Total", typeof(decimal?)), ("RunningTotal", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["A", 4m, 4m], ["B", 2m, 6m]);
    }
}
