using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    #region RANGE BETWEEN with ORDER BY

    [TestMethod]
    public void WhenRangeBetweenUnboundedPrecedingAndCurrentRow_WithOrderBy_ShouldComputeRunningSum()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name range between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m],
            ["Bob", 300m],
            ["Charlie", 600m]);
    }

    [TestMethod]
    public void WhenRangeBetweenWithDistinctValues_ShouldMatchRowsBehavior()
    {
        var query = @"
            select Name,
                   Sum(Population) over (order by Name range between unbounded preceding and current row) as RangeSum,
                   Sum(Population) over (order by Name rows between unbounded preceding and current row) as RowsSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Diana") { Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RangeSum", typeof(decimal)),
            ("RowsSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m, 100m],
            ["Bob", 300m, 300m],
            ["Charlie", 600m, 600m],
            ["Diana", 1000m, 1000m]);
    }

    [TestMethod]
    public void WhenRangeBetweenUnboundedPrecedingAndUnboundedFollowing_ShouldReturnWholePartition()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name range between unbounded preceding and unbounded following) as Total
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        foreach (var row in table)
        {
            Assert.AreEqual(600m, Convert.ToDecimal(row.Values[1]), $"Each row should see whole partition total; Name={row.Values[0]}");
        }
    }

    [TestMethod]
    public void WhenRangeBetweenCurrentRowAndUnboundedFollowing_ShouldComputeReverseRunning()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name range between current row and unbounded following) as RevSum
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

        Assert.AreEqual(600m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(500m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(300m, Convert.ToDecimal(charlie.Values[1]));
    }

    [TestMethod]
    public void WhenRangeBetween_WithPartitionBy_ShouldRespectPartitionBoundaries()
    {
        var query = @"
            select Name, City, Sum(Population) over (partition by City order by Name range between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", Population = 100 },
            new BasicEntity("Bob") { City = "LA", Population = 200 },
            new BasicEntity("Charlie") { City = "NYC", Population = 300 },
            new BasicEntity("Diana") { City = "LA", Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[2]));
        Assert.AreEqual(400m, Convert.ToDecimal(charlie.Values[2]));
        Assert.AreEqual(200m, Convert.ToDecimal(bob.Values[2]));
        Assert.AreEqual(600m, Convert.ToDecimal(diana.Values[2]));
    }

    [TestMethod]
    public void WhenRangeBetween_WithCount_ShouldCountWithinRange()
    {
        var query = @"
            select Name, Count(Name) over (order by Name range between unbounded preceding and current row) as RunCount
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

        Assert.AreEqual(1, Convert.ToInt32(alice.Values[1]));
        Assert.AreEqual(2, Convert.ToInt32(bob.Values[1]));
        Assert.AreEqual(3, Convert.ToInt32(charlie.Values[1]));
    }

    [TestMethod]
    public void WhenRangeBetween_WithAvg_ShouldComputeRunningAverage()
    {
        var query = @"
            select Name, Avg(Population) over (order by Name range between unbounded preceding and current row) as RunAvg
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

        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(150m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(200m, Convert.ToDecimal(charlie.Values[1]));
    }

    [TestMethod]
    public void WhenRangeBetween_WithMinMax_ShouldTrackExtremes()
    {
        var query = @"
            select Name,
                   Min(Population) over (order by Name range between unbounded preceding and current row) as RunMin,
                   Max(Population) over (order by Name range between unbounded preceding and current row) as RunMax
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 300 },
            new BasicEntity("Bob") { Population = 100 },
            new BasicEntity("Charlie") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.AreEqual(300m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(300m, Convert.ToDecimal(alice.Values[2]));

        Assert.AreEqual(100m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(300m, Convert.ToDecimal(bob.Values[2]));

        Assert.AreEqual(100m, Convert.ToDecimal(charlie.Values[1]));
        Assert.AreEqual(300m, Convert.ToDecimal(charlie.Values[2]));
    }

    #endregion
}
