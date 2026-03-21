using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunction_ValueAccessTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenFirstValueOverOrderByName_ShouldReturnFirstInPartition()
    {
        var query = "select Name, FirstValue(Name) over (order by Name) as FV from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        foreach (var row in table)
            Assert.AreEqual("Alice", row.Values[1]);
    }

    [TestMethod]
    public void WhenFirstValueWithPartition_ShouldReturnFirstPerPartition()
    {
        var query = @"
            select Name, City, FirstValue(Name) over (partition by City order by Name) as FV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Diana") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var la = table.Where(r => (string)r.Values[1] == "LA").ToList();
        var nyc = table.Where(r => (string)r.Values[1] == "NYC").ToList();

        foreach (var row in la)
            Assert.AreEqual("Alice", row.Values[2]);

        foreach (var row in nyc)
            Assert.AreEqual("Bob", row.Values[2]);
    }

    [TestMethod]
    public void WhenFirstValueOnNumericColumn_ShouldReturnFirstValue()
    {
        var query = @"
            select Name, FirstValue(Population) over (order by Name) as FV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        foreach (var row in table)
            Assert.AreEqual(100m, row.Values[1]);
    }

    [TestMethod]
    public void WhenLastValueOverOrderByName_ShouldReturnRunningLast()
    {
        var query = "select Name, LastValue(Name) over (order by Name) as LV from #A.entities()";

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

        Assert.AreEqual("Alice", alice.Values[1]);
        Assert.AreEqual("Bob", bob.Values[1]);
        Assert.AreEqual("Charlie", charlie.Values[1]);
    }

    [TestMethod]
    public void WhenLastValueWithPartition_ShouldReturnRunningLastPerPartition()
    {
        var query = @"
            select Name, City, LastValue(Name) over (partition by City order by Name) as LV
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

        Assert.AreEqual("Alice", alice.Values[2]);
        Assert.AreEqual("Diana", diana.Values[2]);
        Assert.AreEqual("Bob", bob.Values[2]);
        Assert.AreEqual("Charlie", charlie.Values[2]);
    }

    [TestMethod]
    public void WhenLastValueWithoutOrderBy_ShouldReturnPartitionLast()
    {
        var query = @"
            select Name, LastValue(Population) over () as LV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var lastPopulation = table[0].Values[1];
        foreach (var row in table)
            Assert.AreEqual(lastPopulation, row.Values[1]);
    }

    [TestMethod]
    public void WhenNthValueWithN2_ShouldReturnSecondValue()
    {
        var query = @"
            select Name, NthValue(Name, 2) over (order by Name) as NV
            from #A.entities()";

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
        Assert.AreEqual("Bob", bob.Values[1]);
        Assert.AreEqual("Bob", charlie.Values[1]);
    }

    [TestMethod]
    public void WhenNthValueWithPartition_ShouldReturnNthPerPartition()
    {
        var query = @"
            select Name, City, NthValue(Name, 2) over (partition by City order by Name) as NV
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

        Assert.IsNull(alice.Values[2]);
        Assert.AreEqual("Diana", diana.Values[2]);
        Assert.IsNull(bob.Values[2]);
        Assert.AreEqual("Charlie", charlie.Values[2]);
    }

    [TestMethod]
    public void WhenNthValueExceedsPartitionSize_ShouldReturnNull()
    {
        var query = @"
            select Name, NthValue(Name, 10) over (order by Name) as NV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        foreach (var row in table)
            Assert.IsNull(row.Values[1]);
    }

    [TestMethod]
    public void WhenFirstValueWithUnderscoreSyntax_ShouldWork()
    {
        var query = "select Name, First_Value(Name) over (order by Name) as FV from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        foreach (var row in table)
            Assert.AreEqual("Alice", row.Values[1]);
    }

    [TestMethod]
    public void WhenNthValueWithN1_ShouldBehaveLikeFirstValue()
    {
        var query = @"
            select Name, 
                   NthValue(Name, 1) over (order by Name) as NV,
                   FirstValue(Name) over (order by Name) as FV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        foreach (var row in table)
        {
            Assert.AreEqual(row.Values[2], row.Values[1]);
            Assert.AreEqual("Alice", row.Values[1]);
        }
    }
}
