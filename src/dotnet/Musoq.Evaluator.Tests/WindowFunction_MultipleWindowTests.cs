using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunction_MultipleWindowTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenTwoWindowsInSelect_ShouldComputeBothIndependently()
    {
        var query = @"
            select Name,
                   RowNumber() over (order by Name) as RowNum,
                   Sum(Population) over (order by Name) as RunSum
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.AreEqual(1L, alice.Values[1]);
        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[2]));
        Assert.AreEqual(2L, bob.Values[1]);
        Assert.AreEqual(300m, Convert.ToDecimal(bob.Values[2]));
        Assert.AreEqual(3L, charlie.Values[1]);
        Assert.AreEqual(600m, Convert.ToDecimal(charlie.Values[2]));
    }

    [TestMethod]
    public void WhenThreeRankingWindows_ShouldComputeAllCorrectly()
    {
        var query = @"
            select Name,
                   RowNumber() over (order by Population) as RowNum,
                   Rank() over (order by Population) as RankVal,
                   DenseRank() over (order by Population) as DenseVal
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 },
            new BasicEntity("Diana") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(1L, alice.Values[1]);
        Assert.AreEqual(1L, alice.Values[2]);
        Assert.AreEqual(1L, alice.Values[3]);

        var diana = table.Single(r => (string)r.Values[0] == "Diana");
        Assert.AreEqual(4L, diana.Values[1]);
        Assert.AreEqual(4L, diana.Values[2]);
        Assert.AreEqual(3L, diana.Values[3]);

        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.AreEqual(2L, bob.Values[2]);
        Assert.AreEqual(2L, charlie.Values[2]);
        Assert.AreEqual(2L, bob.Values[3]);
        Assert.AreEqual(2L, charlie.Values[3]);
    }

    [TestMethod]
    public void WhenSameFunctionDifferentPartitions_ShouldComputeSeparately()
    {
        var query = @"
            select Name, City,
                   Sum(Population) over (partition by City) as CityTotal,
                   Sum(Population) over () as GrandTotal
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var laRows = table.Where(r => (string)r.Values[1] == "LA").ToList();
        Assert.AreEqual(400m, Convert.ToDecimal(laRows[0].Values[2]));
        Assert.AreEqual(600m, Convert.ToDecimal(laRows[0].Values[3]));

        var nycRows = table.Where(r => (string)r.Values[1] == "NYC").ToList();
        Assert.AreEqual(200m, Convert.ToDecimal(nycRows[0].Values[2]));
        Assert.AreEqual(600m, Convert.ToDecimal(nycRows[0].Values[3]));
    }

    [TestMethod]
    public void WhenLagAndLeadTogether_ShouldComputeBothCorrectly()
    {
        var query = @"
            select Name,
                   Lag(Population) over (order by Name) as PrevPop,
                   Lead(Population) over (order by Name) as NextPop
            from #A.entities()";

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

        Assert.IsNull(alice.Values[1]);
        Assert.AreEqual(200m, Convert.ToDecimal(alice.Values[2]));

        Assert.AreEqual(100m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(300m, Convert.ToDecimal(bob.Values[2]));

        Assert.AreEqual(200m, Convert.ToDecimal(charlie.Values[1]));
        Assert.IsNull(charlie.Values[2]);
    }

    [TestMethod]
    public void WhenWindowFunctionOnExpressionArgument_ShouldComputeCorrectly()
    {
        var query = @"
            select Name, Sum(Population * 2) over (order by Name) as DoubledRunSum
            from #A.entities()";

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

        Assert.AreEqual(200m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(600m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(1200m, Convert.ToDecimal(charlie.Values[1]));
    }

    [TestMethod]
    public void WhenWindowAliasInOuterOrderBy_ShouldSortByRunningSum()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name) as RunSum
            from #A.entities()
            order by RunSum desc";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Charlie", table[0].Values[0]);
        Assert.AreEqual(600m, Convert.ToDecimal(table[0].Values[1]));
        Assert.AreEqual("Bob", table[1].Values[0]);
        Assert.AreEqual(300m, Convert.ToDecimal(table[1].Values[1]));
        Assert.AreEqual("Alice", table[2].Values[0]);
        Assert.AreEqual(100m, Convert.ToDecimal(table[2].Values[1]));
    }

    [TestMethod]
    public void WhenMultipleOrderByColumnsInWindow_ShouldSortByAll()
    {
        var query = @"
            select Name, City, RowNumber() over (order by City, Name) as RowNum
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Diana") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.AreEqual(1L, alice.Values[2]);
        Assert.AreEqual(2L, diana.Values[2]);
        Assert.AreEqual(3L, bob.Values[2]);
        Assert.AreEqual(4L, charlie.Values[2]);
    }
}
