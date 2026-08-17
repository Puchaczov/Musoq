using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    [TestMethod]
    public void WhenRowsBetweenUnboundedPrecedingAndCurrentRow_ShouldComputeRunningSum()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name rows between unbounded preceding and current row) as RunSum
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
    public void WhenRowsBetweenOnePrecedingAndOneFollowing_ShouldComputeSlidingWindow()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name rows between 1 preceding and 1 following) as SlideSum
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
            ("SlideSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 300m],
            ["Bob", 600m],
            ["Charlie", 500m]);
    }

    [TestMethod]
    public void WhenRowsBetweenTwoPrecedingAndCurrentRow_ShouldComputeMovingSum()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name rows between 2 preceding and current row) as MovSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Diana") { Population = 400 },
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("MovSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m],
            ["Bob", 300m],
            ["Charlie", 600m],
            ["Diana", 900m]);
    }

    [TestMethod]
    public void WhenRowsBetweenCurrentRowAndUnboundedFollowing_ShouldComputeReverseRunningSum()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name rows between current row and unbounded following) as RevSum
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
            ("RevSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 600m],
            ["Bob", 500m],
            ["Charlie", 300m]);
    }

    [TestMethod]
    public void WhenRowsBetweenUnboundedPrecedingAndUnboundedFollowing_ShouldComputeWholePartition()
    {
        var query = @"
            select Name, Sum(Population) over (order by Name rows between unbounded preceding and unbounded following) as TotalSum
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
            ("TotalSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 600m],
            ["Bob", 600m],
            ["Charlie", 600m]);
    }

    [TestMethod]
    public void WhenRowsFrameWithPartition_ShouldRespectPartitionBoundaries()
    {
        var query = @"
            select City, Name, Sum(Population) over (partition by City order by Name rows between 1 preceding and 1 following) as SlideSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC", Population = 300 },
            new BasicEntity("Alice") { City = "LA", Population = 100 },
            new BasicEntity("Bob") { City = "NYC", Population = 200 },
            new BasicEntity("Diana") { City = "LA", Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Name", typeof(string)),
            ("SlideSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["LA", "Alice", 500m],
            ["LA", "Diana", 500m],
            ["NYC", "Bob", 500m],
            ["NYC", "Charlie", 500m]);
    }

    [TestMethod]
    public void WhenRowsFrameWithCount_ShouldCountWithinFrame()
    {
        var query = @"
            select Name, Count(Name) over (order by Name rows between 1 preceding and 1 following) as FrameCount
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Diana"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("FrameCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 2],
            ["Bob", 3],
            ["Charlie", 3],
            ["Diana", 2]);
    }

    [TestMethod]
    public void WhenSlidingSumOver8Rows_ShouldComputeCorrectFrameForEachRow()
    {
        var query = @"
            select Name, Population,
                   Sum(Population) over (order by Name rows between 1 preceding and 1 following) as SlideSum
            from #A.Entities()";

        // Sorted by Name: A(10), B(20), C(30), D(40), E(50), F(60), G(70), H(80)
        var sources = CreateSingleSource(
            new BasicEntity("E") { Population = 50 },
            new BasicEntity("A") { Population = 10 },
            new BasicEntity("H") { Population = 80 },
            new BasicEntity("C") { Population = 30 },
            new BasicEntity("F") { Population = 60 },
            new BasicEntity("B") { Population = 20 },
            new BasicEntity("G") { Population = 70 },
            new BasicEntity("D") { Population = 40 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)),
            ("SlideSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 10m, 30m], ["B", 20m, 60m], ["C", 30m, 90m], ["D", 40m, 120m],
            ["E", 50m, 150m], ["F", 60m, 180m], ["G", 70m, 210m], ["H", 80m, 150m]);
    }

    [TestMethod]
    public void WhenMovingAvgOver8Rows_ShouldComputeCorrectAverage()
    {
        var query = @"
            select Name,
                   Avg(Population) over (order by Name rows between 2 preceding and current row) as MovAvg
            from #A.Entities()";

        // Sorted: A(10), B(20), C(30), D(40), E(50), F(60), G(70), H(80)
        var sources = CreateSingleSource(
            new BasicEntity("E") { Population = 50 },
            new BasicEntity("A") { Population = 10 },
            new BasicEntity("H") { Population = 80 },
            new BasicEntity("C") { Population = 30 },
            new BasicEntity("F") { Population = 60 },
            new BasicEntity("B") { Population = 20 },
            new BasicEntity("G") { Population = 70 },
            new BasicEntity("D") { Population = 40 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("MovAvg", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 10m], ["B", 15m], ["C", 20m], ["D", 30m],
            ["E", 40m], ["F", 50m], ["G", 60m], ["H", 70m]);
    }

    [TestMethod]
    public void WhenSlidingMinOver8Rows_ShouldComputeCorrectMinimum()
    {
        var query = @"
            select Name,
                   Min(Population) over (order by Name rows between 1 preceding and 1 following) as SlideMin
            from #A.Entities()";

        // Sorted: A(10), B(20), C(30), D(40), E(50), F(60), G(70), H(80)
        var sources = CreateSingleSource(
            new BasicEntity("E") { Population = 50 },
            new BasicEntity("A") { Population = 10 },
            new BasicEntity("H") { Population = 80 },
            new BasicEntity("C") { Population = 30 },
            new BasicEntity("F") { Population = 60 },
            new BasicEntity("B") { Population = 20 },
            new BasicEntity("G") { Population = 70 },
            new BasicEntity("D") { Population = 40 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("SlideMin", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 10m], ["B", 10m], ["C", 20m], ["D", 30m],
            ["E", 40m], ["F", 50m], ["G", 60m], ["H", 70m]);
    }

    [TestMethod]
    public void WhenSlidingMaxOver8Rows_ShouldComputeCorrectMaximum()
    {
        var query = @"
            select Name,
                   Max(Population) over (order by Name rows between 1 preceding and 1 following) as SlideMax
            from #A.Entities()";

        // Sorted: A(10), B(20), C(30), D(40), E(50), F(60), G(70), H(80)
        var sources = CreateSingleSource(
            new BasicEntity("E") { Population = 50 },
            new BasicEntity("A") { Population = 10 },
            new BasicEntity("H") { Population = 80 },
            new BasicEntity("C") { Population = 30 },
            new BasicEntity("F") { Population = 60 },
            new BasicEntity("B") { Population = 20 },
            new BasicEntity("G") { Population = 70 },
            new BasicEntity("D") { Population = 40 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("SlideMax", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 20m], ["B", 30m], ["C", 40m], ["D", 50m],
            ["E", 60m], ["F", 70m], ["G", 80m], ["H", 80m]);
    }

    [TestMethod]
    public void WhenSlidingMinMaxOverNullableValues_ShouldSkipNulls()
    {
        var query = @"
            select Name,
                   Min(NullableValue) over (order by Name rows between 1 preceding and current row) as RollingMin,
                   Max(NullableValue) over (order by Name rows between 1 preceding and current row) as RollingMax
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("A") { NullableValue = null },
            new BasicEntity("B") { NullableValue = 3 },
            new BasicEntity("C") { NullableValue = null },
            new BasicEntity("D") { NullableValue = 1 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RollingMin", typeof(int?)),
            ("RollingMax", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", null, null],
            ["B", 3, 3],
            ["C", 3, 3],
            ["D", 1, 1]);
    }

    [TestMethod]
    public void WhenPrecedingOnlyMinMaxOverNullableValues_ShouldReturnNullForEmptyFrames()
    {
        var query = @"
            select Name,
                   Min(NullableValue) over (order by Name rows between 2 preceding and 1 preceding) as PreviousMin,
                   Max(NullableValue) over (order by Name rows between 2 preceding and 1 preceding) as PreviousMax
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("A") { NullableValue = null },
            new BasicEntity("B") { NullableValue = 3 },
            new BasicEntity("C") { NullableValue = null },
            new BasicEntity("D") { NullableValue = 1 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("PreviousMin", typeof(int?)),
            ("PreviousMax", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", null, null],
            ["B", null, null],
            ["C", 3, 3],
            ["D", 3, 3]);
    }

    [TestMethod]
    public void WhenLargeOffsetExceedsPartitionSize_ShouldClampToBoundaries()
    {
        var query = @"
            select Name,
                   Sum(Population) over (order by Name rows between 10 preceding and 10 following) as WideSum
            from #A.Entities()";

        // Only 4 rows — offset 10 exceeds partition in both directions
        var sources = CreateSingleSource(
            new BasicEntity("A") { Population = 10 },
            new BasicEntity("B") { Population = 20 },
            new BasicEntity("C") { Population = 30 },
            new BasicEntity("D") { Population = 40 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("WideSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 100m], ["B", 100m], ["C", 100m], ["D", 100m]);
    }

    [TestMethod]
    public void WhenThreePrecedingToCurrentRow_Over10Rows_ShouldComputeCorrectly()
    {
        var query = @"
            select Name,
                   Sum(Population) over (order by Name rows between 3 preceding and current row) as MovSum
            from #A.Entities()";

        // Sorted: A(1), B(2), C(3), D(4), E(5), F(6), G(7), H(8), I(9), J(10)
        var sources = CreateSingleSource(
            new BasicEntity("F") { Population = 6 },
            new BasicEntity("A") { Population = 1 },
            new BasicEntity("J") { Population = 10 },
            new BasicEntity("C") { Population = 3 },
            new BasicEntity("H") { Population = 8 },
            new BasicEntity("B") { Population = 2 },
            new BasicEntity("I") { Population = 9 },
            new BasicEntity("E") { Population = 5 },
            new BasicEntity("G") { Population = 7 },
            new BasicEntity("D") { Population = 4 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("MovSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 1m], ["B", 3m], ["C", 6m], ["D", 10m], ["E", 14m],
            ["F", 18m], ["G", 22m], ["H", 26m], ["I", 30m], ["J", 34m]);
    }

    [TestMethod]
    public void WhenMultiplePartitionsWithVaryingSizes_ShouldComputeFramePerPartition()
    {
        var query = @"
            select City, Name,
                   Sum(Population) over (partition by City order by Name rows between 1 preceding and 1 following) as SlideSum
            from #A.Entities()";

        // NYC: A(10), C(30), E(50) — 3 rows
        // LA:  B(20), D(40)        — 2 rows
        // SF:  F(60)               — 1 row (single-element partition)
        var sources = CreateSingleSource(
            new BasicEntity("E") { City = "NYC", Population = 50 },
            new BasicEntity("A") { City = "NYC", Population = 10 },
            new BasicEntity("D") { City = "LA", Population = 40 },
            new BasicEntity("B") { City = "LA", Population = 20 },
            new BasicEntity("F") { City = "SF", Population = 60 },
            new BasicEntity("C") { City = "NYC", Population = 30 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Name", typeof(string)),
            ("SlideSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["NYC", "A", 40m], ["NYC", "C", 90m], ["NYC", "E", 80m],
            ["LA", "B", 60m], ["LA", "D", 60m], ["SF", "F", 60m]);
    }

    [TestMethod]
    public void WhenCurrentRowToCurrentRow_ShouldReturnEachRowsOwnValue()
    {
        var query = @"
            select Name,
                   Sum(Population) over (order by Name rows between current row and current row) as SelfSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("C") { Population = 30 },
            new BasicEntity("A") { Population = 10 },
            new BasicEntity("D") { Population = 40 },
            new BasicEntity("B") { Population = 20 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("SelfSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 10m], ["B", 20m], ["C", 30m], ["D", 40m]);
    }

    [TestMethod]
    public void WhenSingleRowPartition_FrameShouldClampToThatRow()
    {
        var query = @"
            select Name,
                   Sum(Population) over (order by Name rows between 3 preceding and 3 following) as WideSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Solo") { Population = 42 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("WideSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Solo", 42m]);
    }

    [TestMethod]
    public void WhenCountFrameOver10Rows_ShouldMatchExpectedCounts()
    {
        var query = @"
            select Name,
                   Count(Name) over (order by Name rows between 2 preceding and 1 following) as FrameCount
            from #A.Entities()";

        // Sorted: A, B, C, D, E, F, G, H
        var sources = CreateSingleSource(
            new BasicEntity("F"),
            new BasicEntity("A"),
            new BasicEntity("H"),
            new BasicEntity("C"),
            new BasicEntity("E"),
            new BasicEntity("B"),
            new BasicEntity("G"),
            new BasicEntity("D"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("FrameCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 2], ["B", 3], ["C", 4], ["D", 4],
            ["E", 4], ["F", 4], ["G", 4], ["H", 3]);
    }

    [TestMethod]
    public void WhenRunningSumAndSlidingSumTogether_ShouldComputeIndependently()
    {
        var query = @"
            select Name,
                   Sum(Population) over (order by Name rows between unbounded preceding and current row) as RunSum,
                   Sum(Population) over (order by Name rows between 1 preceding and 1 following) as SlideSum
            from #A.Entities()";

        // Sorted: A(10), B(20), C(30), D(40), E(50)
        var sources = CreateSingleSource(
            new BasicEntity("D") { Population = 40 },
            new BasicEntity("A") { Population = 10 },
            new BasicEntity("E") { Population = 50 },
            new BasicEntity("B") { Population = 20 },
            new BasicEntity("C") { Population = 30 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RunSum", typeof(decimal)),
            ("SlideSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 10m, 30m],
            ["B", 30m, 60m],
            ["C", 60m, 90m],
            ["D", 100m, 120m],
            ["E", 150m, 90m]);
    }

}
