using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunction_RowsSemanticsTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    // ========================================================================
    // ROWS Semantics Tests — Tied ORDER BY Values
    // ========================================================================
    // Musoq uses ROWS semantics (row-by-row accumulation), not RANGE semantics
    // (peer-group accumulation). These tests verify the documented behavior
    // from spec section 11.11.2.

    [TestMethod]
    public void WhenRunningSumWithTiedOrderByValues_ShouldAccumulatePerRow()
    {
        // ROWS semantics: each tied row accumulates independently.
        // City is the ORDER BY key; Bob and Charlie both have City="NYC".
        // Sorted: Alice(LA,100) → two NYC rows in some intra-tie order → Diana(SF,400)
        // Under RANGE semantics, both NYC rows would get the same sum. Under ROWS, they differ.
        var query = @"
            select Name, Sum(Population) over (order by City) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC", Population = 300 },
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Diana") { City = "SF", Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(1000m, Convert.ToDecimal(diana.Values[1]));

        // The two NYC rows must get DIFFERENT running sums (ROWS semantics).
        // Under RANGE semantics, they would both get 600 (100+200+300).
        var nycSums = table.Where(r =>
        {
            var name = (string)r.Values[0];
            return name == "Bob" || name == "Charlie";
        }).Select(r => Convert.ToDecimal(r.Values[1]))
            .OrderBy(v => v).ToList();

        Assert.HasCount(2, nycSums);
        Assert.AreNotEqual(nycSums[0], nycSums[1]);

        // Total at each NYC row: first NYC gets 100+first_pop, second gets 100+both_pops.
        // Regardless of intra-tie order, the two sums must be {100+200, 100+200+300}
        // or {100+300, 100+300+200} — either way the sorted pair is the smaller and 600.
        Assert.AreEqual(600m, nycSums[1]);
    }

    [TestMethod]
    public void WhenRunningCountWithTiedOrderByValues_ShouldCountPerRow()
    {
        // Three rows with City="NYC" — running count should be 1,2,3 not all 3.
        var query = @"
            select Name, Count(Name) over (order by City) as RunCount
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Diana") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(1, Convert.ToInt32(alice.Values[1]));

        // NYC rows: under ROWS semantics, counts are 2, 3, 4 (not all 4).
        var nycCounts = table
            .Where(r => (string)r.Values[0] != "Alice")
            .Select(r => Convert.ToInt32(r.Values[1]))
            .OrderBy(v => v).ToList();

        Assert.HasCount(3, nycCounts);
        Assert.AreEqual(2, nycCounts[0]);
        Assert.AreEqual(3, nycCounts[1]);
        Assert.AreEqual(4, nycCounts[2]);
    }

    [TestMethod]
    public void WhenRunningAvgWithTiedOrderByValues_ShouldComputePerRow()
    {
        // avg changes per row even for tied ORDER BY values.
        var query = @"
            select Name, Avg(Population) over (order by City) as RunAvg
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "NYC", Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        // Sorted by City: Alice(LA,100) → Bob(NYC,200) → Charlie(NYC,400)
        // Running avg: 100/1=100, 300/2=150, 700/3≈233.33
        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[1]));

        var nycAvgs = table
            .Where(r => (string)r.Values[0] != "Alice")
            .Select(r => Convert.ToDecimal(r.Values[1]))
            .OrderBy(v => v).ToList();

        Assert.AreEqual(150m, nycAvgs[0]);
        Assert.AreEqual(Math.Round(700m / 3m, 6), Math.Round(nycAvgs[1], 6));
    }

    [TestMethod]
    public void WhenRunningMinWithTiedOrderByValues_ShouldTrackPerRow()
    {
        var query = @"
            select Name, Min(Population) over (order by City) as RunMin
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 50 },
            new BasicEntity("Bob") { City = "NYC", Population = 300 },
            new BasicEntity("Charlie") { City = "NYC", Population = 100 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        // Sorted: Alice(LA,50) → then two NYC rows in some order.
        // Running min: 50, then min(50,first_nyc), then min(50,first_nyc,second_nyc)
        // All running mins should be 50 since Alice is smallest.
        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(50m, Convert.ToDecimal(alice.Values[1]));

        // Both NYC rows: running min is still 50 (Alice's value persists).
        var nycMins = table
            .Where(r => (string)r.Values[0] != "Alice")
            .Select(r => Convert.ToDecimal(r.Values[1])).ToList();

        Assert.IsTrue(nycMins.All(v => v == 50m));
    }

    [TestMethod]
    public void WhenRunningMaxWithTiedOrderByValues_ShouldTrackPerRow()
    {
        var query = @"
            select Name, Max(Population) over (order by City) as RunMax
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 50 },
            new BasicEntity("Bob") { City = "NYC", Population = 300 },
            new BasicEntity("Charlie") { City = "NYC", Population = 100 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        // Sorted: Alice(LA,50) → two NYC rows.
        // Running max: 50, then grows as NYC rows accumulate.
        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(50m, Convert.ToDecimal(alice.Values[1]));

        // One NYC row should have max=300, the other depends on order.
        // Both should be >= 50 and at least one should be 300.
        var nycMaxes = table
            .Where(r => (string)r.Values[0] != "Alice")
            .Select(r => Convert.ToDecimal(r.Values[1]))
            .OrderBy(v => v).ToList();

        Assert.HasCount(2, nycMaxes);
        Assert.AreEqual(300m, nycMaxes[1]);
    }

    [TestMethod]
    public void WhenRunningSumPartitionedWithTiedOrderByValues_ShouldAccumulatePerRowPerPartition()
    {
        // Tied ORDER BY within partitions — ROWS semantics applies per partition.
        var query = @"
            select Name, Country, Sum(Population) over (partition by Country order by City) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Country = "US", City = "NYC", Population = 100 },
            new BasicEntity("Bob") { Country = "US", City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { Country = "US", City = "SF", Population = 300 },
            new BasicEntity("Diana") { Country = "UK", City = "London", Population = 400 },
            new BasicEntity("Eve") { Country = "UK", City = "London", Population = 500 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);

        // US partition sorted by City: NYC(100), NYC(200), SF(300)
        // Running sums: 100, 300, 600 — the two NYC rows get different sums.
        var usRows = table.Where(r => (string)r.Values[1] == "US")
            .Select(r => Convert.ToDecimal(r.Values[2]))
            .OrderBy(v => v).ToList();

        Assert.HasCount(3, usRows);
        Assert.AreEqual(100m, usRows[0]);
        Assert.AreEqual(300m, usRows[1]);
        Assert.AreEqual(600m, usRows[2]);

        // UK partition sorted by City: London(400), London(500)
        // Running sums: 400, 900 — different despite same ORDER BY value.
        var ukSums = table.Where(r => (string)r.Values[1] == "UK")
            .Select(r => Convert.ToDecimal(r.Values[2]))
            .OrderBy(v => v).ToList();

        Assert.HasCount(2, ukSums);
        Assert.AreEqual(400m, ukSums[0]);
        Assert.AreEqual(900m, ukSums[1]);
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

        Assert.AreEqual(3, table.Count);

        // All have same City, so order within ties is non-deterministic.
        // But one row should have NULL (first), and each other row should
        // have a non-null value from the previous row.
        var nullCount = table.Count(r => r.Values[1] == null);
        Assert.AreEqual(1, nullCount);

        var nonNullRows = table.Where(r => r.Values[1] != null).ToList();
        Assert.HasCount(2, nonNullRows);

        // Each non-null LAG value should be one of the Population values.
        var validPops = new HashSet<decimal> { 100m, 200m, 300m };
        Assert.IsTrue(nonNullRows.All(r => validPops.Contains(Convert.ToDecimal(r.Values[1]))));
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

        Assert.AreEqual(3, table.Count);

        // One row (the last) should have NULL LEAD, others should have non-null.
        var nullCount = table.Count(r => r.Values[1] == null);
        Assert.AreEqual(1, nullCount);

        var nonNullRows = table.Where(r => r.Values[1] != null).ToList();
        Assert.HasCount(2, nonNullRows);

        var validPops = new HashSet<decimal> { 100m, 200m, 300m };
        Assert.IsTrue(nonNullRows.All(r => validPops.Contains(Convert.ToDecimal(r.Values[1]))));
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
            select Name, LastValue(Population) over (order by City) as LV
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "NYC", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        // Under ROWS semantics, each row's LAST_VALUE is its own Population.
        // All three LV values should be distinct (each row gets its own value).
        var lvValues = table.Select(r => Convert.ToDecimal(r.Values[1])).OrderBy(v => v).ToList();
        Assert.AreEqual(3, lvValues.Distinct().Count());
        CollectionAssert.AreEquivalent(new[] { 100m, 200m, 300m }, lvValues);
    }

    [TestMethod]
    public void WhenNthValueWithTiedOrderByValues_ShouldUseRowPosition()
    {
        // NTH_VALUE(col, 2) should return the 2nd accumulated row, not 2nd peer group.
        var query = @"
            select Name, NthValue(Population, 2) over (order by City) as NV
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "NYC", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        // Sorted: Alice(LA,100) → two NYC rows in some order.
        // NTH_VALUE(Pop, 2): Alice gets NULL (only 1 row seen), both NYC rows get 2nd value.
        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.IsNull(alice.Values[1]);

        // Both NYC rows should have a non-null NTH_VALUE — the Population of the 2nd row.
        var nycRows = table.Where(r => (string)r.Values[0] != "Alice").ToList();
        Assert.HasCount(2, nycRows);
        Assert.IsTrue(nycRows.All(r => r.Values[1] != null));
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

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(1L, alice.Values[1]);

        // Both NYC rows should have rank 2 (tied).
        var nycRanks = table.Where(r =>
        {
            var name = (string)r.Values[0];
            return name == "Bob" || name == "Charlie";
        }).Select(r => (long)r.Values[1]).ToList();

        Assert.HasCount(2, nycRanks);
        Assert.IsTrue(nycRanks.All(r => r == 2L));

        // Diana should have rank 4 (gap after two rank-2 rows).
        var diana = table.Single(r => (string)r.Values[0] == "Diana");
        Assert.AreEqual(4L, diana.Values[1]);
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

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(1L, alice.Values[1]);

        var nycRanks = table.Where(r =>
        {
            var name = (string)r.Values[0];
            return name == "Bob" || name == "Charlie";
        }).Select(r => (long)r.Values[1]).ToList();

        Assert.IsTrue(nycRanks.All(r => r == 2L));

        // Diana should have dense rank 3 (no gap).
        var diana = table.Single(r => (string)r.Values[0] == "Diana");
        Assert.AreEqual(3L, diana.Values[1]);
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

        Assert.AreEqual(3, table.Count);

        // All rows tied on City, but RowNumber still produces 1, 2, 3.
        var rowNums = table.Select(r => (long)r.Values[1]).OrderBy(v => v).ToList();
        Assert.AreEqual(1L, rowNums[0]);
        Assert.AreEqual(2L, rowNums[1]);
        Assert.AreEqual(3L, rowNums[2]);
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

        Assert.AreEqual(3, table.Count);

        // ASC: NULLs first → Bob(null)=1, Charlie(LA)=2, Alice(NYC)=3.
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        Assert.AreEqual(1L, bob.Values[1]);

        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        Assert.AreEqual(2L, charlie.Values[1]);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(3L, alice.Values[1]);
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

        Assert.AreEqual(3, table.Count);

        // DESC: NULLs last → Alice(NYC)=1, Charlie(LA)=2, Bob(null)=3.
        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(1L, alice.Values[1]);

        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        Assert.AreEqual(2L, charlie.Values[1]);

        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        Assert.AreEqual(3L, bob.Values[1]);
    }

    [TestMethod]
    public void WhenMultipleNullsInOrderByColumnAsc_ShouldGroupNullsFirst()
    {
        var query = @"
            select Name, Sum(Population) over (order by City) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = null, Population = 100 },
            new BasicEntity("Bob") { City = null, Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        // ASC, NULLs first: null-rows come first, then LA.
        // Running sum for null rows: 100, 300 (ROWS semantics — different values).
        // Charlie(LA): 100+200+300 = 600.
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        Assert.AreEqual(600m, Convert.ToDecimal(charlie.Values[1]));

        var nullSums = table.Where(r => (string)r.Values[0] != "Charlie")
            .Select(r => Convert.ToDecimal(r.Values[1]))
            .OrderBy(v => v).ToList();

        Assert.HasCount(2, nullSums);
        Assert.AreEqual(100m, nullSums[0]);
        Assert.AreEqual(300m, nullSums[1]);
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

        Assert.AreEqual(3, table.Count);

        // Two NULL rows are peers → both get rank 1.
        var nullRanks = table.Where(r => (string)r.Values[0] != "Charlie")
            .Select(r => (long)r.Values[1]).ToList();

        Assert.HasCount(2, nullRanks);
        Assert.IsTrue(nullRanks.All(r => r == 1L));

        // Charlie(LA) → rank 3 (gap after two rank-1 rows).
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        Assert.AreEqual(3L, charlie.Values[1]);
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

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.All(r => Convert.ToDecimal(r.Values[1]) == 500m));
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

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.All(r => Convert.ToInt32(r.Values[1]) == 3));
    }
}
