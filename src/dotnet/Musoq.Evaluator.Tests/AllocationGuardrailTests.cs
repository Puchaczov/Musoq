using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Runtime allocation regression guardrails for the grouped-aggregate and window
///     execution paths exercised by GroupByAggregationBenchmark and WindowFunctionBenchmark.
///     Measured baselines are ~9 bytes/row (grouped aggregate) and ~116 bytes/row (window);
///     the ceilings keep margin for variance while still catching order-of-magnitude
///     regressions such as reintroduced per-row boxing or wrapper allocations.
/// </summary>
[TestClass]
public sealed class AllocationGuardrailTests : BasicEntityTestBase
{
    private const int RowCount = 2000;
    private const long GroupByMaxBytesPerRow = 256;
    private const long WindowMaxBytesPerRow = 1024;

    [TestMethod]
    public void GroupedAggregate_WhenExecuted_ShouldStayWithinAllocationBudget()
    {
        var allocatedPerRow = MeasureAllocationPerRow(
            "select Country, Count(Country), Sum(Population) from #A.entities() group by Country");

        Assert.IsLessThanOrEqualTo(
            GroupByMaxBytesPerRow,
            allocatedPerRow,
            $"Grouped-aggregate execution allocated {allocatedPerRow} bytes/row, exceeding the {GroupByMaxBytesPerRow} bytes/row guardrail.");
    }

    [TestMethod]
    public void PartitionedWindow_WhenExecuted_ShouldStayWithinAllocationBudget()
    {
        var allocatedPerRow = MeasureAllocationPerRow(
            "select Name, RowNumber() over (partition by Country order by Population desc) as rn from #A.entities()");

        Assert.IsLessThanOrEqualTo(
            WindowMaxBytesPerRow,
            allocatedPerRow,
            $"Partitioned-window execution allocated {allocatedPerRow} bytes/row, exceeding the {WindowMaxBytesPerRow} bytes/row guardrail.");
    }

    private long MeasureAllocationPerRow(string query)
    {
        var compiled = CreateAndRunVirtualMachine(
            query,
            BuildSources(),
            new CompilationOptions(ParallelizationMode.None));

        compiled.Run();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        compiled.Run();
        var after = GC.GetAllocatedBytesForCurrentThread();

        return (after - before) / RowCount;
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> BuildSources()
    {
        var rows = Enumerable.Range(0, RowCount)
            .Select(index => new BasicEntity($"City{index % 50}", $"Country{index % 8}", index))
            .ToList();

        return new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", rows } };
    }
}
