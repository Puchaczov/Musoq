using System;
using System.Linq;
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

        Assert.AreEqual(4, table.Count);

        // Sorted by Name: Alice(100), Bob(200), Charlie(300), Diana(400)
        // Alice:   frame [Alice] = 100
        // Bob:     frame [Alice, Bob] = 300
        // Charlie: frame [Alice, Bob, Charlie] = 600
        // Diana:   frame [Bob, Charlie, Diana] = 900
        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(300m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(600m, Convert.ToDecimal(charlie.Values[1]));
        Assert.AreEqual(900m, Convert.ToDecimal(diana.Values[1]));
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

        Assert.AreEqual(3, table.Count);

        // Sorted by Name: Alice(100), Bob(200), Charlie(300)
        // Alice:   frame [Alice, Bob, Charlie] = 600
        // Bob:     frame [Bob, Charlie] = 500
        // Charlie: frame [Charlie] = 300
        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.AreEqual(600m, Convert.ToDecimal(alice.Values[1]));
        Assert.AreEqual(500m, Convert.ToDecimal(bob.Values[1]));
        Assert.AreEqual(300m, Convert.ToDecimal(charlie.Values[1]));
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

        Assert.AreEqual(3, table.Count);

        // All rows get the same total = 600
        foreach (var row in table)
            Assert.AreEqual(600m, Convert.ToDecimal(row.Values[1]));
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

        Assert.AreEqual(4, table.Count);

        // LA partition (sorted by Name): Alice(100), Diana(400)
        // Alice: frame [Alice, Diana] = 500
        // Diana: frame [Alice, Diana] = 500
        var aliceLa = table.Single(r => (string)r.Values[1] == "Alice");
        var dianaLa = table.Single(r => (string)r.Values[1] == "Diana");

        Assert.AreEqual(500m, Convert.ToDecimal(aliceLa.Values[2]));
        Assert.AreEqual(500m, Convert.ToDecimal(dianaLa.Values[2]));

        // NYC partition (sorted by Name): Bob(200), Charlie(300)
        // Bob: frame [Bob, Charlie] = 500
        // Charlie: frame [Bob, Charlie] = 500
        var bobNyc = table.Single(r => (string)r.Values[1] == "Bob");
        var charlieNyc = table.Single(r => (string)r.Values[1] == "Charlie");

        Assert.AreEqual(500m, Convert.ToDecimal(bobNyc.Values[2]));
        Assert.AreEqual(500m, Convert.ToDecimal(charlieNyc.Values[2]));
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

        Assert.AreEqual(4, table.Count);

        // Sorted by Name: Alice, Bob, Charlie, Diana
        // Alice:   frame [Alice, Bob] = count 2
        // Bob:     frame [Alice, Bob, Charlie] = count 3
        // Charlie: frame [Bob, Charlie, Diana] = count 3
        // Diana:   frame [Charlie, Diana] = count 2
        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var diana = table.Single(r => (string)r.Values[0] == "Diana");

        Assert.AreEqual(2, Convert.ToInt32(alice.Values[1]));
        Assert.AreEqual(3, Convert.ToInt32(bob.Values[1]));
        Assert.AreEqual(3, Convert.ToInt32(charlie.Values[1]));
        Assert.AreEqual(2, Convert.ToInt32(diana.Values[1]));
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

        Assert.AreEqual(8, table.Count);

        // A: frame [A,B]         = 10+20 = 30
        // B: frame [A,B,C]       = 10+20+30 = 60
        // C: frame [B,C,D]       = 20+30+40 = 90
        // D: frame [C,D,E]       = 30+40+50 = 120
        // E: frame [D,E,F]       = 40+50+60 = 150
        // F: frame [E,F,G]       = 50+60+70 = 180
        // G: frame [F,G,H]       = 60+70+80 = 210
        // H: frame [G,H]         = 70+80 = 150
        AssertWindowResult(table, "A", 30m);
        AssertWindowResult(table, "B", 60m);
        AssertWindowResult(table, "C", 90m);
        AssertWindowResult(table, "D", 120m);
        AssertWindowResult(table, "E", 150m);
        AssertWindowResult(table, "F", 180m);
        AssertWindowResult(table, "G", 210m);
        AssertWindowResult(table, "H", 150m);
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

        Assert.AreEqual(8, table.Count);

        // A: frame [A]         = avg(10)       = 10
        // B: frame [A,B]       = avg(10,20)    = 15
        // C: frame [A,B,C]     = avg(10,20,30) = 20
        // D: frame [B,C,D]     = avg(20,30,40) = 30
        // E: frame [C,D,E]     = avg(30,40,50) = 40
        // F: frame [D,E,F]     = avg(40,50,60) = 50
        // G: frame [E,F,G]     = avg(50,60,70) = 60
        // H: frame [F,G,H]     = avg(60,70,80) = 70
        AssertWindowResult(table, "A", 10m);
        AssertWindowResult(table, "B", 15m);
        AssertWindowResult(table, "C", 20m);
        AssertWindowResult(table, "D", 30m);
        AssertWindowResult(table, "E", 40m);
        AssertWindowResult(table, "F", 50m);
        AssertWindowResult(table, "G", 60m);
        AssertWindowResult(table, "H", 70m);
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

        Assert.AreEqual(8, table.Count);

        // A: frame [A,B]     = min(10,20) = 10
        // B: frame [A,B,C]   = min(10,20,30) = 10
        // C: frame [B,C,D]   = min(20,30,40) = 20
        // D: frame [C,D,E]   = min(30,40,50) = 30
        // E: frame [D,E,F]   = min(40,50,60) = 40
        // F: frame [E,F,G]   = min(50,60,70) = 50
        // G: frame [F,G,H]   = min(60,70,80) = 60
        // H: frame [G,H]     = min(70,80) = 70
        AssertWindowResult(table, "A", 10m);
        AssertWindowResult(table, "B", 10m);
        AssertWindowResult(table, "C", 20m);
        AssertWindowResult(table, "D", 30m);
        AssertWindowResult(table, "E", 40m);
        AssertWindowResult(table, "F", 50m);
        AssertWindowResult(table, "G", 60m);
        AssertWindowResult(table, "H", 70m);
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

        Assert.AreEqual(8, table.Count);

        // A: frame [A,B]     = max(10,20) = 20
        // B: frame [A,B,C]   = max(10,20,30) = 30
        // C: frame [B,C,D]   = max(20,30,40) = 40
        // D: frame [C,D,E]   = max(30,40,50) = 50
        // E: frame [D,E,F]   = max(40,50,60) = 60
        // F: frame [E,F,G]   = max(50,60,70) = 70
        // G: frame [F,G,H]   = max(60,70,80) = 80
        // H: frame [G,H]     = max(70,80) = 80
        AssertWindowResult(table, "A", 20m);
        AssertWindowResult(table, "B", 30m);
        AssertWindowResult(table, "C", 40m);
        AssertWindowResult(table, "D", 50m);
        AssertWindowResult(table, "E", 60m);
        AssertWindowResult(table, "F", 70m);
        AssertWindowResult(table, "G", 80m);
        AssertWindowResult(table, "H", 80m);
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

        Assert.AreEqual(4, table.Count);

        var a = table.Single(r => (string)r.Values[0] == "A");
        Assert.IsNull(a.Values[1]);
        Assert.IsNull(a.Values[2]);

        var b = table.Single(r => (string)r.Values[0] == "B");
        Assert.AreEqual(3, Convert.ToInt32(b.Values[1]));
        Assert.AreEqual(3, Convert.ToInt32(b.Values[2]));

        var c = table.Single(r => (string)r.Values[0] == "C");
        Assert.AreEqual(3, Convert.ToInt32(c.Values[1]));
        Assert.AreEqual(3, Convert.ToInt32(c.Values[2]));

        var d = table.Single(r => (string)r.Values[0] == "D");
        Assert.AreEqual(1, Convert.ToInt32(d.Values[1]));
        Assert.AreEqual(1, Convert.ToInt32(d.Values[2]));
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

        Assert.AreEqual(4, table.Count);

        var a = table.Single(r => (string)r.Values[0] == "A");
        Assert.IsNull(a.Values[1]);
        Assert.IsNull(a.Values[2]);

        var b = table.Single(r => (string)r.Values[0] == "B");
        Assert.IsNull(b.Values[1]);
        Assert.IsNull(b.Values[2]);

        var c = table.Single(r => (string)r.Values[0] == "C");
        Assert.AreEqual(3, Convert.ToInt32(c.Values[1]));
        Assert.AreEqual(3, Convert.ToInt32(c.Values[2]));

        var d = table.Single(r => (string)r.Values[0] == "D");
        Assert.AreEqual(3, Convert.ToInt32(d.Values[1]));
        Assert.AreEqual(3, Convert.ToInt32(d.Values[2]));
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

        Assert.AreEqual(4, table.Count);

        // All rows should see the entire partition sum = 100
        AssertWindowResult(table, "A", 100m);
        AssertWindowResult(table, "B", 100m);
        AssertWindowResult(table, "C", 100m);
        AssertWindowResult(table, "D", 100m);
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

        Assert.AreEqual(10, table.Count);

        // A: [A]           = 1
        // B: [A,B]         = 3
        // C: [A,B,C]       = 6
        // D: [A,B,C,D]     = 10
        // E: [B,C,D,E]     = 14
        // F: [C,D,E,F]     = 18
        // G: [D,E,F,G]     = 22
        // H: [E,F,G,H]     = 26
        // I: [F,G,H,I]     = 30
        // J: [G,H,I,J]     = 34
        AssertWindowResult(table, "A", 1m);
        AssertWindowResult(table, "B", 3m);
        AssertWindowResult(table, "C", 6m);
        AssertWindowResult(table, "D", 10m);
        AssertWindowResult(table, "E", 14m);
        AssertWindowResult(table, "F", 18m);
        AssertWindowResult(table, "G", 22m);
        AssertWindowResult(table, "H", 26m);
        AssertWindowResult(table, "I", 30m);
        AssertWindowResult(table, "J", 34m);
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

        Assert.AreEqual(6, table.Count);

        // NYC partition (A=10, C=30, E=50):
        //   A: [A,C]     = 10+30 = 40
        //   C: [A,C,E]   = 10+30+50 = 90
        //   E: [C,E]     = 30+50 = 80
        AssertPartitionedWindowResult(table, "NYC", "A", 40m);
        AssertPartitionedWindowResult(table, "NYC", "C", 90m);
        AssertPartitionedWindowResult(table, "NYC", "E", 80m);

        // LA partition (B=20, D=40):
        //   B: [B,D] = 20+40 = 60
        //   D: [B,D] = 20+40 = 60
        AssertPartitionedWindowResult(table, "LA", "B", 60m);
        AssertPartitionedWindowResult(table, "LA", "D", 60m);

        // SF partition (F=60): single row
        //   F: [F] = 60
        AssertPartitionedWindowResult(table, "SF", "F", 60m);
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

        Assert.AreEqual(4, table.Count);

        AssertWindowResult(table, "A", 10m);
        AssertWindowResult(table, "B", 20m);
        AssertWindowResult(table, "C", 30m);
        AssertWindowResult(table, "D", 40m);
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42m, Convert.ToDecimal(table[0].Values[1]));
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

        Assert.AreEqual(8, table.Count);

        // A: [A,B]         = 2  (no preceding available)
        // B: [A,B,C]       = 3  (only 1 preceding available)
        // C: [A,B,C,D]     = 4
        // D: [B,C,D,E]     = 4
        // E: [C,D,E,F]     = 4
        // F: [D,E,F,G]     = 4
        // G: [E,F,G,H]     = 4
        // H: [F,G,H]       = 3  (no following available)
        AssertWindowIntResult(table, "A", 2);
        AssertWindowIntResult(table, "B", 3);
        AssertWindowIntResult(table, "C", 4);
        AssertWindowIntResult(table, "D", 4);
        AssertWindowIntResult(table, "E", 4);
        AssertWindowIntResult(table, "F", 4);
        AssertWindowIntResult(table, "G", 4);
        AssertWindowIntResult(table, "H", 3);
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

        Assert.AreEqual(5, table.Count);

        // Running sum:  A=10, B=30, C=60, D=100, E=150
        // Sliding sum:  A=30, B=60, C=90, D=120, E=90
        var a = table.Single(r => (string)r.Values[0] == "A");
        var b = table.Single(r => (string)r.Values[0] == "B");
        var c = table.Single(r => (string)r.Values[0] == "C");
        var d = table.Single(r => (string)r.Values[0] == "D");
        var e = table.Single(r => (string)r.Values[0] == "E");

        Assert.AreEqual(10m, Convert.ToDecimal(a.Values[1]));
        Assert.AreEqual(30m, Convert.ToDecimal(a.Values[2]));

        Assert.AreEqual(30m, Convert.ToDecimal(b.Values[1]));
        Assert.AreEqual(60m, Convert.ToDecimal(b.Values[2]));

        Assert.AreEqual(60m, Convert.ToDecimal(c.Values[1]));
        Assert.AreEqual(90m, Convert.ToDecimal(c.Values[2]));

        Assert.AreEqual(100m, Convert.ToDecimal(d.Values[1]));
        Assert.AreEqual(120m, Convert.ToDecimal(d.Values[2]));

        Assert.AreEqual(150m, Convert.ToDecimal(e.Values[1]));
        Assert.AreEqual(90m, Convert.ToDecimal(e.Values[2]));
    }

}
