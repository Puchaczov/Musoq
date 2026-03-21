using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunction_IntegrationTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenWindowInsideCte_ShouldAllowFilteringByRowNumber()
    {
        var query = @"
            with ranked as (
                select Name, RowNumber() over (order by Name) as RowNum from #A.entities()
            )
            select Name, RowNum from ranked where RowNum <= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Charlie"),
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob"),
                    new BasicEntity("Diana")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Alice" && Convert.ToInt64(r.Values[1]) == 1L));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Bob" && Convert.ToInt64(r.Values[1]) == 2L));
    }

    [TestMethod]
    public void WhenWindowOverCteSource_ShouldComputeWindowValues()
    {
        var query = @"
            with p as (
                select City, Country from #A.entities()
            )
            select City, Country, RowNumber() over (order by City) from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var berlin = table.Single(r => (string)r.Values[0] == "BERLIN");
        var munich = table.Single(r => (string)r.Values[0] == "MUNICH");
        var warsaw = table.Single(r => (string)r.Values[0] == "WARSAW");

        Assert.AreEqual(1L, berlin.Values[2]);
        Assert.AreEqual(2L, munich.Values[2]);
        Assert.AreEqual(3L, warsaw.Values[2]);
    }

    [TestMethod]
    public void WhenWindowOverAggregatedCte_ShouldComputeRunningTotal()
    {
        var query = @"
            with agg as (
                select City, Sum(Population) as CityPop from #A.entities() group by City
            )
            select City, CityPop, Sum(CityPop) over (order by City) as RunningPop from agg";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "Berlin", Population = 100 },
                    new BasicEntity("Bob") { City = "Berlin", Population = 200 },
                    new BasicEntity("Charlie") { City = "Munich", Population = 300 },
                    new BasicEntity("Diana") { City = "Warsaw", Population = 150 },
                    new BasicEntity("Eve") { City = "Warsaw", Population = 250 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var berlin = table.Single(r => (string)r.Values[0] == "Berlin");
        var munich = table.Single(r => (string)r.Values[0] == "Munich");
        var warsaw = table.Single(r => (string)r.Values[0] == "Warsaw");

        Assert.AreEqual(300m, Convert.ToDecimal(berlin.Values[1]));
        Assert.AreEqual(300m, Convert.ToDecimal(berlin.Values[2]));

        Assert.AreEqual(300m, Convert.ToDecimal(munich.Values[1]));
        Assert.AreEqual(600m, Convert.ToDecimal(munich.Values[2]));

        Assert.AreEqual(400m, Convert.ToDecimal(warsaw.Values[1]));
        Assert.AreEqual(1000m, Convert.ToDecimal(warsaw.Values[2]));
    }

    [TestMethod]
    public void WhenWindowOverInnerJoin_ShouldNumberJoinedRows()
    {
        var query = @"
            select a.Name, b.City, RowNumber() over (order by a.Name) as RowNum
            from #A.entities() a
            inner join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                new BasicEntity("Alice") { Id = 1 },
                new BasicEntity("Bob") { Id = 2 },
                new BasicEntity("Charlie") { Id = 3 }
            ]},
            { "#B", [
                new BasicEntity("x") { Id = 1, City = "NYC" },
                new BasicEntity("y") { Id = 2, City = "LA" },
                new BasicEntity("z") { Id = 4, City = "SF" }
            ]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");

        Assert.AreEqual("NYC", alice.Values[1]);
        Assert.AreEqual(1L, alice.Values[2]);
        Assert.AreEqual("LA", bob.Values[1]);
        Assert.AreEqual(2L, bob.Values[2]);
    }

    [TestMethod]
    public void WhenWindowPartitionByJoinedColumn_ShouldPartitionCorrectly()
    {
        var query = @"
            select a.Name, b.City, RowNumber() over (partition by b.City order by a.Name) as RowNum
            from #A.entities() a
            inner join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                new BasicEntity("Alice") { Id = 1 },
                new BasicEntity("Bob") { Id = 2 },
                new BasicEntity("Charlie") { Id = 3 },
                new BasicEntity("Diana") { Id = 4 }
            ]},
            { "#B", [
                new BasicEntity("x") { Id = 1, City = "NYC" },
                new BasicEntity("y") { Id = 2, City = "LA" },
                new BasicEntity("z") { Id = 3, City = "NYC" },
                new BasicEntity("w") { Id = 4, City = "LA" }
            ]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var nycRows = table.Where(r => (string)r.Values[1] == "NYC")
            .OrderBy(r => (long)r.Values[2]).ToList();
        Assert.HasCount(2, nycRows);
        Assert.AreEqual("Alice", nycRows[0].Values[0]);
        Assert.AreEqual(1L, nycRows[0].Values[2]);
        Assert.AreEqual("Charlie", nycRows[1].Values[0]);
        Assert.AreEqual(2L, nycRows[1].Values[2]);

        var laRows = table.Where(r => (string)r.Values[1] == "LA")
            .OrderBy(r => (long)r.Values[2]).ToList();
        Assert.HasCount(2, laRows);
        Assert.AreEqual("Bob", laRows[0].Values[0]);
        Assert.AreEqual(1L, laRows[0].Values[2]);
        Assert.AreEqual("Diana", laRows[1].Values[0]);
        Assert.AreEqual(2L, laRows[1].Values[2]);
    }

    [TestMethod]
    public void WhenOuterOrderByWindowAlias_ShouldSortByWindowResult()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum
            from #A.entities()
            order by RowNum desc";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Charlie", table[0].Values[0]);
        Assert.AreEqual(3L, table[0].Values[1]);
        Assert.AreEqual("Bob", table[1].Values[0]);
        Assert.AreEqual(2L, table[1].Values[1]);
        Assert.AreEqual("Alice", table[2].Values[0]);
        Assert.AreEqual(1L, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenWindowWithSkip_ShouldSkipRows()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum
            from #A.entities()
            skip 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void WhenWindowWithTake_ShouldLimitRows()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum
            from #A.entities()
            take 2";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void WhenWindowWithSkipAndTake_ShouldPaginate()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as RowNum
            from #A.entities()
            order by RowNum asc
            skip 1
            take 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Bob", table[0].Values[0]);
        Assert.AreEqual(2L, table[0].Values[1]);
    }

    [TestMethod]
    public void WhenDistinctWithWindow_ShouldDeduplicateWindowResults()
    {
        var query = @"
            select distinct City, Count(Name) over (partition by City) as CityCount
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "LA" },
            new BasicEntity("Diana") { City = "NYC" },
            new BasicEntity("Eve") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var la = table.Single(r => (string)r.Values[0] == "LA");
        var nyc = table.Single(r => (string)r.Values[0] == "NYC");

        Assert.AreEqual(2, Convert.ToInt32(la.Values[1]));
        Assert.AreEqual(3, Convert.ToInt32(nyc.Values[1]));
    }

    [TestMethod]
    public void WhenCaseWhenOnWindowResult_ShouldEvaluateCorrectly()
    {
        var query = @"
            select Name,
                   RowNumber() over (order by Name) as RowNum,
                   case when RowNumber() over (order by Name) <= 2 then 'Top' else 'Bottom' end as Category
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"),
            new BasicEntity("Diana"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual("Top", alice.Values[2]);
        Assert.AreEqual("Top", bob.Values[2]);
        Assert.AreEqual("Bottom", charlie.Values[2]);
        Assert.AreEqual("Bottom", diana.Values[2]);
    }

    [TestMethod]
    public void WhenReorderedSyntaxWithWindow_ShouldWork()
    {
        var query = "from #A.entities() select Name, RowNumber() over (order by Name) as RowNum";

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

        Assert.AreEqual(1L, alice.Values[1]);
        Assert.AreEqual(2L, bob.Values[1]);
        Assert.AreEqual(3L, charlie.Values[1]);
    }
}
