using System;
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
        var query = "select Name, RowNumber() over (order by Name) as rn from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 2L],
            ["Charlie", 3L]);
    }

    [TestMethod]
    public void WhenRowNumberOverPartitionByCity_ShouldNumberWithinPartitions()
    {
        var query =
            "select Name, City, RowNumber() over (partition by City order by Name) as rn from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC", Population = 300 },
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Diana") { City = "LA", Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", 1L],
            ["Diana", "LA", 2L],
            ["Bob", "NYC", 1L],
            ["Charlie", "NYC", 2L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RunningTotal", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m],
            ["Bob", 300m],
            ["Charlie", 600m]);
    }

    [TestMethod]
    public void WhenCountOverPartitionByCity_ShouldCountPerPartition()
    {
        var query = "select Name, City, Count(Name) over (partition by City) as CityCount from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 },
            new BasicEntity("Diana") { City = "NYC", Population = 400 },
            new BasicEntity("Eve") { City = "NYC", Population = 500 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("CityCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", 2],
            ["Charlie", "LA", 2],
            ["Bob", "NYC", 3],
            ["Diana", "NYC", 3],
            ["Eve", "NYC", 3]);
    }

    [TestMethod]
    public void WhenCountOverPartitionByCityWithNullValues_ShouldIgnoreNullCountedValues()
    {
        var query = "select Name, City, Count(Name) over (partition by City) as CityCount from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity { Name = null, City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity { Name = null, City = "NYC" },
            new BasicEntity("Charlie") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("CityCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", 1],
            [null, "LA", 1],
            ["Bob", "NYC", 2],
            [null, "NYC", 2],
            ["Charlie", "NYC", 2]);
    }

    [TestMethod]
    public void WhenRankOverOrderByPopulation_ShouldHandleTies()
    {
        var query = "select Name, Rank() over (order by Population) as rnk from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 },
            new BasicEntity("Diana") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("rnk", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 2L],
            ["Charlie", 2L],
            ["Diana", 4L]);
    }

    [TestMethod]
    public void WhenDenseRankOverOrderByPopulation_ShouldNotSkipRanks()
    {
        var query = "select Name, DenseRank() over (order by Population) as dense_rnk from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 },
            new BasicEntity("Diana") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("dense_rnk", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 2L],
            ["Charlie", 2L],
            ["Diana", 3L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("CityTotal", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", 400m],
            ["Charlie", "LA", 400m],
            ["Bob", "NYC", 200m]);
    }

    [TestMethod]
    public void WhenRowNumberOverOrderByNameDesc_ShouldUseDescendingOrder()
    {
        var query = "select Name, RowNumber() over (order by Name desc) as rn from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 3L],
            ["Bob", 2L],
            ["Charlie", 1L]);
    }

    [TestMethod]
    public void WhenWindowFunctionWithWhereClause_ShouldFilterBeforeWindowing()
    {
        var query =
            "select Name, RowNumber() over (order by Name) as rn from #A.Entities() where Population > 150";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Diana") { Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", 1L],
            ["Charlie", 2L],
            ["Diana", 3L]);
    }

    [TestMethod]
    public void WhenLagOverOrderByName_ShouldReturnPreviousValue()
    {
        var query =
            "select Name, Lag(Population) over (order by Name) as previous_population from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("previous_population", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", null],
            ["Bob", 100m],
            ["Charlie", 200m]);
    }

    [TestMethod]
    public void WhenLeadOverOrderByName_ShouldReturnNextValue()
    {
        var query =
            "select Name, Lead(Population) over (order by Name) as next_population from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("next_population", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 200m],
            ["Bob", 300m],
            ["Charlie", null]);
    }

    [TestMethod]
    public void WhenAvgOverOrderByName_ShouldComputeRunningAverage()
    {
        var query =
            "select Name, Avg(Population) over (order by Name) as running_average from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("running_average", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m],
            ["Bob", 150m],
            ["Charlie", 200m]);
    }

    [TestMethod]
    public void WhenMinOverPartitionByCity_ShouldComputePartitionMinimum()
    {
        var query =
            "select Name, City, Min(Population) over (partition by City) as partition_min from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("partition_min", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", 100m],
            ["Charlie", "LA", 100m],
            ["Bob", "NYC", 200m]);
    }

    [TestMethod]
    public void WhenMaxOverPartitionByCity_ShouldComputePartitionMaximum()
    {
        var query =
            "select Name, City, Max(Population) over (partition by City) as partition_max from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Charlie") { City = "LA", Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("partition_max", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", 300m],
            ["Charlie", "LA", 300m],
            ["Bob", "NYC", 200m]);
    }

    [TestMethod]
    public void WhenRowNumberWithUnderscoreForm_ShouldWorkIdentically()
    {
        var query = "select Name, ROW_NUMBER() over (order by Name) as rn from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 2L],
            ["Charlie", 3L]);
    }

    [TestMethod]
    public void WhenDenseRankWithUnderscoreForm_ShouldWorkIdentically()
    {
        var query = "select Name, DENSE_RANK() over (order by Population) as dense_rnk from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 },
            new BasicEntity("Diana") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("dense_rnk", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 2L],
            ["Charlie", 2L],
            ["Diana", 3L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Bucket", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 1L],
            ["Charlie", 1L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Bucket", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 1L],
            ["Charlie", 2L],
            ["Diana", 2L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Bucket", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 1L],
            ["Charlie", 2L],
            ["Diana", 2L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Bucket", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 2L],
            ["Charlie", 3L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("Bucket", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 1L],
            ["Bob", "NYC", 2L],
            ["Charlie", "LA", 1L],
            ["Diana", "LA", 2L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Bucket", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L],
            ["Bob", 1L],
            ["Charlie", 2L],
            ["Diana", 2L],
            ["Eve", 3L]);
    }

    #endregion
}
