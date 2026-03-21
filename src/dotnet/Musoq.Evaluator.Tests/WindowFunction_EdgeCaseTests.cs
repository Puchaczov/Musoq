using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunction_EdgeCaseTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenEmptySource_ShouldReturnEmptyTable()
    {
        var query = "select Name, RowNumber() over (order by Name) from #A.entities()";

        var sources = CreateSingleSource(Array.Empty<BasicEntity>());

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenSingleRow_ShouldHandleWindowFunctions()
    {
        var query = @"
            select Name,
                   RowNumber() over (order by Name) as RowNum,
                   Lag(Population) over (order by Name) as PrevPop,
                   Lead(Population) over (order by Name) as NextPop
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0].Values[0]);
        Assert.AreEqual(1L, table[0].Values[1]);
        Assert.IsNull(table[0].Values[2]);
        Assert.IsNull(table[0].Values[3]);
    }

    [TestMethod]
    public void WhenAllRowsSamePartition_ShouldTreatAsOneGroup()
    {
        var query = @"
            select Name, City, RowNumber() over (partition by City order by Name) as RowNum
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.AreEqual(1L, alice.Values[2]);
        Assert.AreEqual(2L, bob.Values[2]);
        Assert.AreEqual(3L, charlie.Values[2]);
    }

    [TestMethod]
    public void WhenLagOnStringColumn_ShouldReturnPreviousString()
    {
        var query = "select Name, Lag(Name) over (order by Name) as PrevName from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.IsNull(alice.Values[1]);
        Assert.AreEqual("Alice", bob.Values[1]);
        Assert.AreEqual("Bob", charlie.Values[1]);
    }

    [TestMethod]
    public void WhenLeadOnStringColumn_ShouldReturnNextString()
    {
        var query = "select Name, Lead(Name) over (order by Name) as NextName from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.AreEqual("Bob", alice.Values[1]);
        Assert.AreEqual("Charlie", bob.Values[1]);
        Assert.IsNull(charlie.Values[1]);
    }

    [TestMethod]
    public void WhenNullInPartitionColumn_ShouldGroupNullsSeparately()
    {
        var query = @"
            select Name, City, RowNumber() over (partition by City order by Name) as RowNum
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = null },
            new BasicEntity("Charlie") { City = null },
            new BasicEntity("Diana") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var laRows = table.Where(r => (string)r.Values[1] == "LA")
            .OrderBy(r => (long)r.Values[2]).ToList();
        Assert.HasCount(2, laRows);
        Assert.AreEqual(1L, laRows[0].Values[2]);
        Assert.AreEqual(2L, laRows[1].Values[2]);

        var nullRows = table.Where(r => r.Values[1] == null)
            .OrderBy(r => (long)r.Values[2]).ToList();
        Assert.HasCount(2, nullRows);
        Assert.AreEqual(1L, nullRows[0].Values[2]);
        Assert.AreEqual(2L, nullRows[1].Values[2]);
    }

    [TestMethod]
    public void WhenLargeDataset_ShouldProcessCorrectly()
    {
        var entities = Enumerable.Range(1, 100)
            .Select(i => new BasicEntity($"Name{i:D3}") { Population = i })
            .ToArray();

        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum,
                   Sum(Population) over (order by Name) as RunSum
            from #A.entities()";

        var sources = CreateSingleSource(entities);

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(100, table.Count);

        var lastRow = table.Single(r => (string)r.Values[0] == "Name100");
        Assert.AreEqual(100L, lastRow.Values[1]);
        Assert.AreEqual(5050m, Convert.ToDecimal(lastRow.Values[2]));
    }

    [TestMethod]
    public void WhenWhereEliminatesAllRows_ShouldReturnEmptyTable()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum
            from #A.entities()
            where 1 = 0";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenRunningAggregateWithDescOrder_ShouldAccumulateInDescOrder()
    {
        var query = @"
            select Name, Sum(Population) over (order by Population desc) as RunSum
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var alice = table.Single(r => (string)r.Values[0] == "Alice");

        Assert.AreEqual(300m, Convert.ToDecimal(charlie.Values[1]));
        Assert.AreEqual(500m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(600m, Convert.ToDecimal(alice.Values[1]));
    }

    [TestMethod]
    public void WhenCustomRunningProductWindowFunction_ShouldComputeCorrectly()
    {
        var query = @"
            select Name, RunningProduct(Population) over (order by Name) as Product
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 4 },
            new BasicEntity("Alice") { Population = 2 },
            new BasicEntity("Bob") { Population = 3 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        // Ordered by Name: Alice(2) → Bob(3) → Charlie(4)
        Assert.AreEqual(2m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(6m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(24m, Convert.ToDecimal(charlie.Values[1]));
    }

    [TestMethod]
    public void WhenCustomRunningProductWithPartition_ShouldResetPerPartition()
    {
        var query = @"
            select Name, City, RunningProduct(Population) over (partition by City order by Name) as Product
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC", Population = 5 },
            new BasicEntity("Alice") { City = "LA", Population = 2 },
            new BasicEntity("Bob") { City = "NYC", Population = 3 },
            new BasicEntity("Diana") { City = "LA", Population = 4 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var laRows = table.Where(r => (string)r.Values[1] == "LA")
            .OrderBy(r => (string)r.Values[0]).ToList();

        // LA partition ordered by Name: Alice(2) → Diana(4)
        Assert.AreEqual("Alice", laRows[0].Values[0]);
        Assert.AreEqual(2m, Convert.ToDecimal(laRows[0].Values[2]));
        Assert.AreEqual("Diana", laRows[1].Values[0]);
        Assert.AreEqual(8m, Convert.ToDecimal(laRows[1].Values[2]));

        var nycRows = table.Where(r => (string)r.Values[1] == "NYC")
            .OrderBy(r => (string)r.Values[0]).ToList();

        // NYC partition ordered by Name: Bob(3) → Charlie(5)
        Assert.AreEqual("Bob", nycRows[0].Values[0]);
        Assert.AreEqual(3m, Convert.ToDecimal(nycRows[0].Values[2]));
        Assert.AreEqual("Charlie", nycRows[1].Values[0]);
        Assert.AreEqual(15m, Convert.ToDecimal(nycRows[1].Values[2]));
    }

    [TestMethod]
    public void WhenCustomRunningProductWithBuiltInWindow_ShouldComputeBothCorrectly()
    {
        var query = @"
            select Name,
                   RunningProduct(Population) over (order by Name) as Product,
                   Sum(Population) over (order by Name) as RunningSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 4 },
            new BasicEntity("Alice") { Population = 2 },
            new BasicEntity("Bob") { Population = 3 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        // RunningProduct: Alice(2) → Bob(2*3=6) → Charlie(2*3*4=24)
        Assert.AreEqual(2m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(6m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(24m, Convert.ToDecimal(charlie.Values[1]));

        // RunningSum: Alice(2) → Bob(2+3=5) → Charlie(2+3+4=9)
        Assert.AreEqual(2m, Convert.ToDecimal(alice.Values[2]));
        Assert.AreEqual(5m, Convert.ToDecimal(bob.Values[2]));
        Assert.AreEqual(9m, Convert.ToDecimal(charlie.Values[2]));
    }

    [TestMethod]
    public void WhenWindowFunctionInWhereClause_ShouldThrow()
    {
        var query = "select Name from #A.entities() where RowNumber() over (order by Name) = 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        Assert.Throws<Exception>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });
    }

    [TestMethod]
    public void WhenNestedWindowInArgument_ShouldThrow()
    {
        var query = @"
            select Name, Sum(RowNumber() over (order by Name)) over (order by Name)
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        Assert.Throws<Exception>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });
    }

    [TestMethod]
    public void WhenUnsupportedFunctionWithOver_ShouldThrow()
    {
        var query = "select Name, ToUpper(Name) over (order by Name) from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        Assert.Throws<Exception>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });
    }
}
