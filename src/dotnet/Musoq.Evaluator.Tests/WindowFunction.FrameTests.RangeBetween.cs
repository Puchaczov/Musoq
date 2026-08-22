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

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("Total", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 600m], ["Bob", 600m], ["Charlie", 600m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RevSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 600m], ["Bob", 500m], ["Charlie", 300m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)), ("City", typeof(string)), ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 100m], ["Charlie", "NYC", 400m],
            ["Bob", "LA", 200m], ["Diana", "LA", 600m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RunCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1], ["Bob", 2], ["Charlie", 3]);
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

        TableMaterializationTestHelper.AssertColumns(
            table, ("Name", typeof(string)), ("RunAvg", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m], ["Bob", 150m], ["Charlie", 200m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)), ("RunMin", typeof(decimal?)), ("RunMax", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 300m, 300m], ["Bob", 100m, 300m], ["Charlie", 100m, 300m]);
    }

    [TestMethod]
    public void WhenRangeCurrentRowUsesCompositeKeys_ShouldRespectPeersPartitionsDirectionAndNullOrdering()
    {
        const string query = @"
            select Name, City, NullableValue, Country,
                   Sum(Population) over (
                       partition by City
                       order by NullableValue desc nulls last, Country asc nulls first
                       range between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", NullableValue = 2, Country = "US", Population = 10 },
            new BasicEntity("Bob") { City = "NYC", NullableValue = 2, Country = "US", Population = 20 },
            new BasicEntity("Cara") { City = "NYC", NullableValue = 1, Country = "CA", Population = 30 },
            new BasicEntity("Dan") { City = "LA", NullableValue = 5, Country = null, Population = 40 },
            new BasicEntity("Eve") { City = "LA", NullableValue = null, Country = "ZZ", Population = 50 });

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 2, "US", 30m],
            ["Bob", "NYC", 2, "US", 30m],
            ["Cara", "NYC", 1, "CA", 60m],
            ["Dan", "LA", 5, null, 40m],
            ["Eve", "LA", null, "ZZ", 90m]);
    }

    [TestMethod]
    public void WhenBoundedRangeCurrentKeyIsNull_ShouldUseNullPeerGroup()
    {
        const string query = @"
            select Name, NullableValue,
                   Sum(Population) over (
                       order by NullableValue asc nulls last
                       range between 1 preceding and 1 following) as AscSum,
                   Sum(Population) over (
                       order by NullableValue desc nulls first
                       range between 1 preceding and 1 following) as DescSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("NullA") { NullableValue = null, Population = 10 },
            new BasicEntity("NullB") { NullableValue = null, Population = 20 },
            new BasicEntity("One") { NullableValue = 1, Population = 1 },
            new BasicEntity("Two") { NullableValue = 2, Population = 2 });

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["NullA", null, 30m, 30m],
            ["NullB", null, 30m, 30m],
            ["One", 1, 3m, 3m],
            ["Two", 2, 3m, 3m]);
    }

    [TestMethod]
    public void WhenRangeRunsOnEmptyAndSingleRowPartitions_ShouldReturnStableResults()
    {
        const string query = @"
            select Name, Sum(Population) over (
                partition by City
                order by Population
                range between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var emptyTable = CreateAndRunVirtualMachine(query, CreateSingleSource()).Run(TestContext.CancellationToken);
        Assert.AreEqual(0, emptyTable.Count);

        var singleTable = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(new BasicEntity("Only") { City = "Solo", Population = 7 }))
            .Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(singleTable, ["Only", 7m]);
    }

    [TestMethod]
    public void WhenValueAccessUsesImplicitOrExplicitRange_ShouldExposeCompletePeerGroup()
    {
        const string query = @"
            select Name, Population,
                   NthValue(Name, 2) over (order by Population) as ImplicitSecond,
                   NthValue(Name, 2) over (
                       order by Population
                       range between unbounded preceding and current row) as ExplicitSecond
            from #A.Entities()";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(
                new BasicEntity("Alice") { Population = 100 },
                new BasicEntity("Bob") { Population = 100 },
                new BasicEntity("Charlie") { Population = 200 }))
            .Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)),
            ("ImplicitSecond", typeof(string)),
            ("ExplicitSecond", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m, "Bob", "Bob"],
            ["Bob", 100m, "Bob", "Bob"],
            ["Charlie", 200m, "Bob", "Bob"]);
    }

    #endregion
}
