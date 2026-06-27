using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunctionBasicTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenRowNumberOverOrderByName_ShouldAssignSequentialNumbers()
    {
        var query = "select Name, RowNumber() over (order by Name) from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var rows = table.OrderBy(r => (long)r.Values[1]).ToList();

        Assert.AreEqual("Alice", rows[0].Values[0]);
        Assert.AreEqual(1L, rows[0].Values[1]);
        Assert.AreEqual("Bob", rows[1].Values[0]);
        Assert.AreEqual(2L, rows[1].Values[1]);
        Assert.AreEqual("Charlie", rows[2].Values[0]);
        Assert.AreEqual(3L, rows[2].Values[1]);
    }

    [TestMethod]
    public void WhenRowNumberOverPartitionByCity_ShouldNumberWithinPartitions()
    {
        var query =
            "select Name, City, RowNumber() over (partition by City order by Name) from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC", Population = 300 },
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Diana") { City = "LA", Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var laRows = table.Where(r => (string)r.Values[1] == "LA")
            .OrderBy(r => (long)r.Values[2]).ToList();
        Assert.HasCount(2, laRows);
        Assert.AreEqual("Alice", laRows[0].Values[0]);
        Assert.AreEqual(1L, laRows[0].Values[2]);
        Assert.AreEqual("Diana", laRows[1].Values[0]);
        Assert.AreEqual(2L, laRows[1].Values[2]);

        var nycRows = table.Where(r => (string)r.Values[1] == "NYC")
            .OrderBy(r => (long)r.Values[2]).ToList();
        Assert.HasCount(2, nycRows);
        Assert.AreEqual("Bob", nycRows[0].Values[0]);
        Assert.AreEqual(1L, nycRows[0].Values[2]);
        Assert.AreEqual("Charlie", nycRows[1].Values[0]);
        Assert.AreEqual(2L, nycRows[1].Values[2]);
    }

    [TestMethod]
    public void WhenSumOverOrderByName_ShouldComputeRunningSum()
    {
        var query =
            "select Name, Sum(Population) over (order by Name) as RunningTotal from #A.Entities()";
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

        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(300m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(600m, Convert.ToDecimal(charlie.Values[1]));
    }

    [TestMethod]
    public void WhenCountOverPartitionByCity_ShouldCountPerPartition()
    {
        var query = "select Name, City, Count(Name) over (partition by City) from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 },
            new BasicEntity("Diana") { City = "NYC", Population = 400 },
            new BasicEntity("Eve") { City = "NYC", Population = 500 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);

        var laRows = table.Where(r => (string)r.Values[1] == "LA").ToList();
        Assert.HasCount(2, laRows);
        Assert.AreEqual(2, Convert.ToInt32(laRows[0].Values[2]));
        Assert.AreEqual(2, Convert.ToInt32(laRows[1].Values[2]));

        var nycRows = table.Where(r => (string)r.Values[1] == "NYC").ToList();
        Assert.HasCount(3, nycRows);
        Assert.AreEqual(3, Convert.ToInt32(nycRows[0].Values[2]));
        Assert.AreEqual(3, Convert.ToInt32(nycRows[1].Values[2]));
        Assert.AreEqual(3, Convert.ToInt32(nycRows[2].Values[2]));
    }

    [TestMethod]
    public void WhenCountOverPartitionByCityWithNullValues_ShouldIgnoreNullCountedValues()
    {
        var query = "select Name, City, Count(Name) over (partition by City) from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity { Name = null, City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity { Name = null, City = "NYC" },
            new BasicEntity("Charlie") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var laRows = table.Where(r => (string)r.Values[1] == "LA").ToList();
        Assert.HasCount(2, laRows);
        Assert.IsTrue(laRows.All(row => Convert.ToInt32(row.Values[2]) == 1));

        var nycRows = table.Where(r => (string)r.Values[1] == "NYC").ToList();
        Assert.HasCount(3, nycRows);
        Assert.IsTrue(nycRows.All(row => Convert.ToInt32(row.Values[2]) == 2));
    }

    [TestMethod]
    public void WhenRankOverOrderByPopulation_ShouldHandleTies()
    {
        var query = "select Name, Rank() over (order by Population) from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 },
            new BasicEntity("Diana") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual(1L, alice.Values[1]);
        Assert.AreEqual(2L, bob.Values[1]);
        Assert.AreEqual(2L, charlie.Values[1]);
        Assert.AreEqual(4L, diana.Values[1]);
    }

    [TestMethod]
    public void WhenDenseRankOverOrderByPopulation_ShouldNotSkipRanks()
    {
        var query = "select Name, DenseRank() over (order by Population) from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 },
            new BasicEntity("Diana") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual(1L, alice.Values[1]);
        Assert.AreEqual(2L, bob.Values[1]);
        Assert.AreEqual(2L, charlie.Values[1]);
        Assert.AreEqual(3L, diana.Values[1]);
    }

    [TestMethod]
    public void WhenSumOverPartitionByCityNoOrder_ShouldComputePartitionTotal()
    {
        var query =
            "select Name, City, Sum(Population) over (partition by City) as CityTotal from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var laRows = table.Where(r => (string)r.Values[1] == "LA").ToList();
        Assert.HasCount(2, laRows);
        Assert.AreEqual(400m, Convert.ToDecimal(laRows[0].Values[2]));
        Assert.AreEqual(400m, Convert.ToDecimal(laRows[1].Values[2]));

        var nycRows = table.Where(r => (string)r.Values[1] == "NYC").ToList();
        Assert.HasCount(1, nycRows);
        Assert.AreEqual(200m, Convert.ToDecimal(nycRows[0].Values[2]));
    }

    [TestMethod]
    public void WhenRowNumberOverOrderByNameDesc_ShouldUseDescendingOrder()
    {
        var query = "select Name, RowNumber() over (order by Name desc) from #A.Entities()";
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

        Assert.AreEqual(3L, alice.Values[1]);
        Assert.AreEqual(2L, bob.Values[1]);
        Assert.AreEqual(1L, charlie.Values[1]);
    }

    [TestMethod]
    public void WhenWindowFunctionWithWhereClause_ShouldFilterBeforeWindowing()
    {
        var query =
            "select Name, RowNumber() over (order by Name) from #A.Entities() where Population > 150";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Diana") { Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual(1L, bob.Values[1]);
        Assert.AreEqual(2L, charlie.Values[1]);
        Assert.AreEqual(3L, diana.Values[1]);
    }

    [TestMethod]
    public void WhenLagOverOrderByName_ShouldReturnPreviousValue()
    {
        var query =
            "select Name, Lag(Population) over (order by Name) from #A.Entities()";
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

        Assert.IsNull(alice.Values[1]);
        Assert.AreEqual(100m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(200m, Convert.ToDecimal(charlie.Values[1]));
    }

    [TestMethod]
    public void WhenLeadOverOrderByName_ShouldReturnNextValue()
    {
        var query =
            "select Name, Lead(Population) over (order by Name) from #A.Entities()";
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

        Assert.AreEqual(200m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(300m, Convert.ToDecimal(bob.Values[1]));
        Assert.IsNull(charlie.Values[1]);
    }

    [TestMethod]
    public void WhenAvgOverOrderByName_ShouldComputeRunningAverage()
    {
        var query =
            "select Name, Avg(Population) over (order by Name) from #A.Entities()";
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

        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(150m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(200m, Convert.ToDecimal(charlie.Values[1]));
    }

    [TestMethod]
    public void WhenMinOverPartitionByCity_ShouldComputePartitionMinimum()
    {
        var query =
            "select Name, City, Min(Population) over (partition by City) from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var laRows = table.Where(r => (string)r.Values[1] == "LA").ToList();
        Assert.HasCount(2, laRows);
        Assert.AreEqual(100m, Convert.ToDecimal(laRows[0].Values[2]));
        Assert.AreEqual(100m, Convert.ToDecimal(laRows[1].Values[2]));

        var nycRows = table.Where(r => (string)r.Values[1] == "NYC").ToList();
        Assert.HasCount(1, nycRows);
        Assert.AreEqual(200m, Convert.ToDecimal(nycRows[0].Values[2]));
    }

    [TestMethod]
    public void WhenMaxOverPartitionByCity_ShouldComputePartitionMaximum()
    {
        var query =
            "select Name, City, Max(Population) over (partition by City) from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var laRows = table.Where(r => (string)r.Values[1] == "LA").ToList();
        Assert.HasCount(2, laRows);
        Assert.AreEqual(300m, Convert.ToDecimal(laRows[0].Values[2]));
        Assert.AreEqual(300m, Convert.ToDecimal(laRows[1].Values[2]));

        var nycRows = table.Where(r => (string)r.Values[1] == "NYC").ToList();
        Assert.HasCount(1, nycRows);
        Assert.AreEqual(200m, Convert.ToDecimal(nycRows[0].Values[2]));
    }

    [TestMethod]
    public void WhenRowNumberWithUnderscoreForm_ShouldWorkIdentically()
    {
        var query = "select Name, ROW_NUMBER() over (order by Name) from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var rows = table.OrderBy(r => (long)r.Values[1]).ToList();

        Assert.AreEqual("Alice", rows[0].Values[0]);
        Assert.AreEqual(1L, rows[0].Values[1]);
        Assert.AreEqual("Bob", rows[1].Values[0]);
        Assert.AreEqual(2L, rows[1].Values[1]);
        Assert.AreEqual("Charlie", rows[2].Values[0]);
        Assert.AreEqual(3L, rows[2].Values[1]);
    }

    [TestMethod]
    public void WhenDenseRankWithUnderscoreForm_ShouldWorkIdentically()
    {
        var query = "select Name, DENSE_RANK() over (order by Population) from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 },
            new BasicEntity("Diana") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual(1L, alice.Values[1]);
        Assert.AreEqual(2L, bob.Values[1]);
        Assert.AreEqual(2L, charlie.Values[1]);
        Assert.AreEqual(3L, diana.Values[1]);
    }

    #region NTILE Dedicated Tests

    [TestMethod]
    public void WhenNtileWithBucketSize1_ShouldAssignAllToSameBucket()
    {
        var query = "select Name, Ntile(1) over (order by Name) as Bucket from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        foreach (var row in table)
            Assert.AreEqual(1L, row.Values[1]);
    }

    [TestMethod]
    public void WhenNtileWithBucketSize2_ShouldDistributeEvenly()
    {
        var query = "select Name, Ntile(2) over (order by Name) as Bucket from #A.Entities()";
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

        Assert.AreEqual(1L, alice.Values[1]);
        Assert.AreEqual(1L, bob.Values[1]);
        Assert.AreEqual(2L, charlie.Values[1]);
        Assert.AreEqual(2L, diana.Values[1]);
    }

    [TestMethod]
    public void WhenNtileWithUnderscoreSyntax_ShouldWork()
    {
        var query = "select Name, N_Tile(2) over (order by Name) as Bucket from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"),
            new BasicEntity("Diana"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(row => (string)row.Values[0] == "Alice");
        var bob = table.Single(row => (string)row.Values[0] == "Bob");
        var charlie = table.Single(row => (string)row.Values[0] == "Charlie");
        var diana = table.Single(row => (string)row.Values[0] == "Diana");

        Assert.AreEqual(1L, alice.Values[1]);
        Assert.AreEqual(1L, bob.Values[1]);
        Assert.AreEqual(2L, charlie.Values[1]);
        Assert.AreEqual(2L, diana.Values[1]);
    }

    [TestMethod]
    public void WhenNtileWithBucketSizeLargerThanRows_ShouldAssignOnePerBucket()
    {
        var query = "select Name, Ntile(100) over (order by Name) as Bucket from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

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

    [TestMethod]
    public void WhenNtileWithPartitionBy_ShouldDistributeWithinPartitions()
    {
        var query = @"
            select Name, City, Ntile(2) over (partition by City order by Name) as Bucket
            from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "LA" },
            new BasicEntity("Diana") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual(1L, alice.Values[2]);
        Assert.AreEqual(2L, bob.Values[2]);
        Assert.AreEqual(1L, charlie.Values[2]);
        Assert.AreEqual(2L, diana.Values[2]);
    }

    [TestMethod]
    public void WhenNtileWithUnevenDistribution_ShouldDistributeRemaindersToFirstBuckets()
    {
        var query = "select Name, Ntile(3) over (order by Name) as Bucket from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"),
            new BasicEntity("Diana"),
            new BasicEntity("Eve"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");
        var eve = table.Single(r => (string)r.Values[0] == "Eve");

        // 5 rows / 3 buckets = [2, 2, 1]. First two buckets get extra rows
        Assert.AreEqual(1L, alice.Values[1]);
        Assert.AreEqual(1L, bob.Values[1]);
        Assert.AreEqual(2L, charlie.Values[1]);
        Assert.AreEqual(2L, diana.Values[1]);
        Assert.AreEqual(3L, eve.Values[1]);
    }

    #endregion
}
