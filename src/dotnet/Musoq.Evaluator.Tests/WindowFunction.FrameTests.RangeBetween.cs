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

    #endregion
}
