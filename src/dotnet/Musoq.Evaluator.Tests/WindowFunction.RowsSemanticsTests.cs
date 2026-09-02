using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunctionRowsSemanticsTests : BasicEntityTestBase
{

    // ========================================================================
    // ROWS Semantics Tests — Tied ORDER BY Values
    // ========================================================================
    // Explicit ROWS frames use row-by-row accumulation rather than RANGE
    // peer-group expansion. These tests verify spec section 11.11.2.

    [TestMethod]
    public void WhenRunningSumWithTiedOrderByValues_ShouldAccumulatePerRow()
    {
        // ROWS semantics: each tied row accumulates independently.
        // City is the ORDER BY key; Bob and Charlie both have City="NYC".
        // Sorted: Alice(LA,100) → two NYC rows in some intra-tie order → Diana(SF,400)
        // Under RANGE semantics, both NYC rows would get the same sum. Under ROWS, they differ.
        var query = @"
            select Name, Sum(Population) over (
                order by City rows between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC", Population = 300 },
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Diana") { City = "SF", Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m], ["Charlie", 400m], ["Bob", 600m], ["Diana", 1000m]);
    }

    [TestMethod]
    public void WhenRunningCountWithTiedOrderByValues_ShouldCountPerRow()
    {
        // Three rows with City="NYC" — running count should be 1,2,3 not all 3.
        var query = @"
            select Name, Count(Name) over (
                order by City rows between unbounded preceding and current row) as RunCount
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Diana") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RunCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1], ["Bob", 2], ["Charlie", 3], ["Diana", 4]);
    }

    [TestMethod]
    public void WhenRunningAvgWithTiedOrderByValues_ShouldComputePerRow()
    {
        // avg changes per row even for tied ORDER BY values.
        var query = @"
            select Name, Avg(Population) over (
                order by City rows between unbounded preceding and current row) as RunAvg
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "NYC", Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RunAvg", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m], ["Bob", 150m], ["Charlie", 700m / 3m]);
    }

    [TestMethod]
    public void WhenRunningMinWithTiedOrderByValues_ShouldTrackPerRow()
    {
        var query = @"
            select Name, Min(Population) over (
                order by City rows between unbounded preceding and current row) as RunMin
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 50 },
            new BasicEntity("Bob") { City = "NYC", Population = 300 },
            new BasicEntity("Charlie") { City = "NYC", Population = 100 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RunMin", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 50m], ["Bob", 50m], ["Charlie", 50m]);
    }

    [TestMethod]
    public void WhenRunningMaxWithTiedOrderByValues_ShouldTrackPerRow()
    {
        var query = @"
            select Name, Max(Population) over (
                order by City rows between unbounded preceding and current row) as RunMax
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 50 },
            new BasicEntity("Bob") { City = "NYC", Population = 300 },
            new BasicEntity("Charlie") { City = "NYC", Population = 100 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RunMax", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 50m], ["Bob", 300m], ["Charlie", 300m]);
    }

    [TestMethod]
    public void WhenRunningSumPartitionedWithTiedOrderByValues_ShouldAccumulatePerRowPerPartition()
    {
        // Tied ORDER BY within partitions — ROWS semantics applies per partition.
        var query = @"
            select Name, Country, Sum(Population) over (
                partition by Country order by City
                rows between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Country = "US", City = "NYC", Population = 100 },
            new BasicEntity("Bob") { Country = "US", City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { Country = "US", City = "SF", Population = 300 },
            new BasicEntity("Diana") { Country = "UK", City = "London", Population = 400 },
            new BasicEntity("Eve") { Country = "UK", City = "London", Population = 500 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("Country", typeof(string)), ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "US", 100m], ["Bob", "US", 300m], ["Charlie", "US", 600m],
            ["Diana", "UK", 400m], ["Eve", "UK", 900m]);
    }

    // ========================================================================
    // Offset Functions with Ties
    // ========================================================================

    [TestMethod]
    public void WhenLagWithTiedOrderByValues_ShouldOffsetByRowPosition()
    {
        // LAG operates by row position, not by peer group.
        // Three rows with same City — LAG(Population) should go to previous row.
        var query = @"
            select Name, Lag(Population) over (order by City) as PrevPop
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "NYC", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("PrevPop", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", null], ["Bob", 100m], ["Charlie", 200m]);
    }

    [TestMethod]
    public void WhenLeadWithTiedOrderByValues_ShouldOffsetByRowPosition()
    {
        var query = @"
            select Name, Lead(Population) over (order by City) as NextPop
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "NYC", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("NextPop", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 200m], ["Bob", 300m], ["Charlie", null]);
    }

    // ========================================================================
    // Value Access Functions with Ties
    // ========================================================================

    [TestMethod]
    public void WhenLastValueWithTiedOrderByValues_ShouldReturnCurrentRowValue()
    {
        // ROWS frame: LAST_VALUE with ORDER BY returns the current row's value
        // (frame is UNBOUNDED PRECEDING TO CURRENT ROW, so current row is "last").
        var query = @"
            select Name, LastValue(Population) over (
                order by City rows between unbounded preceding and current row) as LV
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "NYC", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("LV", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m], ["Bob", 200m], ["Charlie", 300m]);
    }

    [TestMethod]
    public void WhenNthValueWithTiedOrderByValues_ShouldUseRowPosition()
    {
        // NTH_VALUE(col, 2) should return the 2nd accumulated row, not 2nd peer group.
        var query = @"
            select Name, NthValue(Population, 2) over (
                order by City rows between unbounded preceding and current row) as NV
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "NYC", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("NV", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", null], ["Bob", 200m], ["Charlie", 200m]);
    }

    // ========================================================================
    // Ranking with Ties
    // ========================================================================

    [TestMethod]
    public void WhenRankWithTiedOrderByValues_ShouldAssignSameRankToTies()
    {
        // Rank is unaffected by ROWS vs RANGE — ties get same rank, with gaps.
        var query = @"
            select Name, Rank() over (order by City) as R
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Diana") { City = "SF" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("R", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L], ["Bob", 2L], ["Charlie", 2L], ["Diana", 4L]);
    }

    [TestMethod]
    public void WhenDenseRankWithTiedOrderByValues_ShouldAssignSameRankNoGaps()
    {
        var query = @"
            select Name, DenseRank() over (order by City) as DR
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Diana") { City = "SF" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("DR", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L], ["Bob", 2L], ["Charlie", 2L], ["Diana", 3L]);
    }

    [TestMethod]
    public void WhenRowNumberWithTiedOrderByValues_ShouldAssignDistinctNumbers()
    {
        var query = @"
            select Name, RowNumber() over (order by City) as RN
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RN", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L], ["Bob", 2L], ["Charlie", 3L]);
    }

    // ========================================================================
    // NULL Ordering
    // ========================================================================

    [TestMethod]
    public void WhenNullInOrderByColumnAsc_ShouldSortNullsFirst()
    {
        var query = @"
            select Name, RowNumber() over (order by City) as RN
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = null },
            new BasicEntity("Charlie") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RN", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", 1L], ["Charlie", 2L], ["Alice", 3L]);
    }

    [TestMethod]
    public void WhenNullInOrderByColumnDesc_ShouldSortNullsLast()
    {
        var query = @"
            select Name, RowNumber() over (order by City desc) as RN
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = null },
            new BasicEntity("Charlie") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RN", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L], ["Charlie", 2L], ["Bob", 3L]);
    }

    [TestMethod]
    public void WhenMultipleNullsInOrderByColumnAsc_ShouldGroupNullsFirst()
    {
        var query = @"
            select Name, Sum(Population) over (
                order by City rows between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = null, Population = 100 },
            new BasicEntity("Bob") { City = null, Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m], ["Bob", 300m], ["Charlie", 600m]);
    }

    [TestMethod]
    public void WhenNullInOrderByWithRank_ShouldTreatNullsAsPeers()
    {
        var query = @"
            select Name, Rank() over (order by City) as R
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = null },
            new BasicEntity("Bob") { City = null },
            new BasicEntity("Charlie") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("R", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L], ["Bob", 1L], ["Charlie", 3L]);
    }

    // ========================================================================
    // Whole-partition aggregates (no ORDER BY) — identical to RANGE
    // ========================================================================

    [TestMethod]
    public void WhenSumWithoutOrderBy_ShouldReturnSameValueForAllRows()
    {
        // Without ORDER BY, all rows share the same partition-wide sum regardless of ties.
        var query = @"
            select Name, Sum(Population) over () as Total
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("Total", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 500m], ["Bob", 500m], ["Charlie", 500m]);
    }

    [TestMethod]
    public void WhenCountWithoutOrderBy_ShouldReturnSameCountForAllRows()
    {
        var query = @"
            select Name, Count(Name) over () as Total
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("Total", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 3], ["Bob", 3], ["Charlie", 3]);
    }
}
