using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    [TestMethod]
    public void WhenFrameWithDescOrdering_ShouldComputeCorrectSlidingWindow()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name desc rows between 1 preceding and 1 following) as SlideSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Diana") { Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        // Sorted DESC: Diana(400), Charlie(300), Bob(200), Alice(100)
        // Diana:   frame [Diana, Charlie] = 400 + 300 = 700
        // Charlie: frame [Diana, Charlie, Bob] = 400 + 300 + 200 = 900
        // Bob:     frame [Charlie, Bob, Alice] = 300 + 200 + 100 = 600
        // Alice:   frame [Bob, Alice] = 200 + 100 = 300
        AssertWindowResult(table, "Diana", 700m);
        AssertWindowResult(table, "Charlie", 900m);
        AssertWindowResult(table, "Bob", 600m);
        AssertWindowResult(table, "Alice", 300m);
    }

    [TestMethod]
    public void WhenFrameOverInnerJoin_ShouldComputeAcrossJoinedRows()
    {
        var query = @"
            select a.Name, b.City,
                   Sum(a.Population) over (order by a.Name rows between 1 preceding and current row) as RunSum
            from #A.entities() a
            inner join #B.entities() b on a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                new BasicEntity("Alice") { Population = 100 },
                new BasicEntity("Bob") { Population = 200 },
                new BasicEntity("Charlie") { Population = 300 }
            ]},
            { "#B", [
                new BasicEntity("Alice") { City = "NYC" },
                new BasicEntity("Bob") { City = "LA" },
                new BasicEntity("Charlie") { City = "SF" }
            ]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        // Sorted by Name: Alice(100), Bob(200), Charlie(300)
        // Alice:   frame [Alice] = 100
        // Bob:     frame [Alice, Bob] = 300
        // Charlie: frame [Bob, Charlie] = 500
        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[2]));
        Assert.AreEqual(300m, Convert.ToDecimal(bob.Values[2]));
        Assert.AreEqual(500m, Convert.ToDecimal(charlie.Values[2]));
    }

    [TestMethod]
    public void WhenFrameWithWhereClause_ShouldComputeOverFilteredRows()
    {
        var query = @"
            select Name,
                   Sum(Population) over (order by Name rows between 1 preceding and current row) as RunSum
            from #A.Entities()
            where Population > 100";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Diana") { Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // After WHERE: Bob(200), Charlie(300), Diana(400)
        Assert.AreEqual(3, table.Count);

        // Sorted by Name: Bob(200), Charlie(300), Diana(400)
        // Bob:     frame [Bob] = 200
        // Charlie: frame [Bob, Charlie] = 500
        // Diana:   frame [Charlie, Diana] = 700
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual(200m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(500m, Convert.ToDecimal(charlie.Values[1]));
        Assert.AreEqual(700m, Convert.ToDecimal(diana.Values[1]));
    }

    [TestMethod]
    public void WhenFrameInsideCte_ShouldComputeAndPassThrough()
    {
        var query = @"
            with windowed as (
                select Name, Population,
                       Sum(Population) over (order by Name rows between unbounded preceding and current row) as RunSum
                from #A.entities()
            )
            select Name, RunSum from windowed where RunSum > 300";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Population = 100 },
                    new BasicEntity("Bob") { Population = 200 },
                    new BasicEntity("Charlie") { Population = 300 },
                    new BasicEntity("Diana") { Population = 400 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // Running sums: Alice=100, Bob=300, Charlie=600, Diana=1000
        // WHERE RunSum > 300 → Charlie(600), Diana(1000)
        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Charlie" && Convert.ToDecimal(r.Values[1]) == 600m));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Diana" && Convert.ToDecimal(r.Values[1]) == 1000m));
    }

    [TestMethod]
    public void WhenFrameWithPartitionAndDescOrder_ShouldComputePerPartition()
    {
        var query = @"
            select City, Name,
                   Sum(Population) over (partition by City order by Name desc rows between 1 preceding and current row) as FrameSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 },
            new BasicEntity("Diana") { City = "LA", Population = 400 },
            new BasicEntity("Eve") { City = "LA", Population = 500 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);

        // NYC partition DESC: Bob(200), Alice(100)
        // Bob:   frame [Bob] = 200
        // Alice: frame [Bob, Alice] = 300
        AssertPartitionedWindowResult(table, "NYC", "Bob", 200m);
        AssertPartitionedWindowResult(table, "NYC", "Alice", 300m);

        // LA partition DESC: Eve(500), Diana(400), Charlie(300)
        // Eve:     frame [Eve] = 500
        // Diana:   frame [Eve, Diana] = 900
        // Charlie: frame [Diana, Charlie] = 700
        AssertPartitionedWindowResult(table, "LA", "Eve", 500m);
        AssertPartitionedWindowResult(table, "LA", "Diana", 900m);
        AssertPartitionedWindowResult(table, "LA", "Charlie", 700m);
    }

}
