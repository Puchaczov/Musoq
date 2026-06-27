using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    #region Implicit Frame Defaults

    [TestMethod]
    public void WhenOrderByWithoutFrame_ShouldDefaultToRunningSum()
    {
        // Default with ORDER BY: ROWS UNBOUNDED PRECEDING to CURRENT ROW
        var query = @"
            select Name, Population,
                   Sum(Population) over (order by Name) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[2]));
        Assert.AreEqual(300m, Convert.ToDecimal(bob.Values[2]));
        Assert.AreEqual(600m, Convert.ToDecimal(charlie.Values[2]));
    }

    [TestMethod]
    public void WhenNoOrderByNoFrame_ShouldReturnWholePartitionSum()
    {
        // Default without ORDER BY: ROWS UNBOUNDED PRECEDING to UNBOUNDED FOLLOWING
        var query = @"
            select Name, Population,
                   Sum(Population) over () as TotalSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        foreach (var row in table)
            Assert.AreEqual(600m, Convert.ToDecimal(row.Values[2]));
    }

    [TestMethod]
    public void WhenRowsWithTiedValues_ShouldAccumulatePerRow()
    {
        var query = @"
            select Name, Population,
                   Sum(Population) over (order by Population rows between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 100 },
            new BasicEntity("Charlie") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var sums = table
            .OrderBy(r => Convert.ToDecimal(r.Values[2]))
            .Select(r => Convert.ToDecimal(r.Values[2]))
            .ToList();

        // ROWS: per-row accumulation even with ties — 100, 200, 400
        Assert.AreEqual(100m, sums[0]);
        Assert.AreEqual(200m, sums[1]);
        Assert.AreEqual(400m, sums[2]);
    }

    [TestMethod]
    public void WhenRangeWithTiedValues_ShouldAccumulateLikeRows()
    {
        // RANGE maps to ROWS semantics in Musoq — per-row, not peer-group
        var query = @"
            select Name, Population,
                   Sum(Population) over (order by Population range between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 100 },
            new BasicEntity("Charlie") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var sums = table
            .OrderBy(r => Convert.ToDecimal(r.Values[2]))
            .Select(r => Convert.ToDecimal(r.Values[2]))
            .ToList();

        // RANGE in Musoq behaves like ROWS — per-row accumulation
        Assert.AreEqual(100m, sums[0]);
        Assert.AreEqual(200m, sums[1]);
        Assert.AreEqual(400m, sums[2]);
    }

    #endregion
}
