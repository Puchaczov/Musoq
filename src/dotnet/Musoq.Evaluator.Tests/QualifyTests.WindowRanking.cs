using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class QualifyTests
{

    [TestMethod]
    public void WhenQualifyTopNPerPartition_WithLargerDataset_ShouldReturnCorrectRows()
    {
        var query = @"
            select City, Name, RowNumber() over (partition by City order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (partition by City order by Name) <= 2";

        // NYC: Alice, Bob, Charlie, Dave — keep Alice, Bob
        // LA:  Eve, Frank, Grace        — keep Eve, Frank
        // SF:  Hank                     — keep Hank (only 1)
        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Eve") { City = "LA" },
            new BasicEntity("Hank") { City = "SF" },
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Grace") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Frank") { City = "LA" },
            new BasicEntity("Dave") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "NYC" && (string)r.Values[1] == "Alice"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "NYC" && (string)r.Values[1] == "Bob"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "LA" && (string)r.Values[1] == "Eve"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "LA" && (string)r.Values[1] == "Frank"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "SF" && (string)r.Values[1] == "Hank"));

        Assert.IsFalse(table.Any(r => (string)r.Values[1] == "Charlie"));
        Assert.IsFalse(table.Any(r => (string)r.Values[1] == "Dave"));
        Assert.IsFalse(table.Any(r => (string)r.Values[1] == "Grace"));
    }

    [TestMethod]
    public void WhenQualifyWithRank_ShouldHandleTiesCorrectly()
    {
        var query = @"
            select Name, Population, Rank() over (order by Population desc) as rnk
            from #A.Entities()
            qualify Rank() over (order by Population desc) <= 2";

        // Populations: 500, 400, 400, 300, 200
        // Rank desc: Alice=1, Bob=2, Charlie=2, Dave=4, Eve=5
        // Qualify <= 2 should return 3 rows (Alice, Bob, Charlie)
        var sources = CreateSingleSource(
            new BasicEntity("Dave") { Population = 300 },
            new BasicEntity("Alice") { Population = 500 },
            new BasicEntity("Charlie") { Population = 400 },
            new BasicEntity("Eve") { Population = 200 },
            new BasicEntity("Bob") { Population = 400 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Alice"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Bob"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Charlie"));
    }

    [TestMethod]
    public void WhenQualifyWithNtile_ShouldFilterByBucket()
    {
        var query = @"
            select Name, Ntile(3) over (order by Name) as bucket
            from #A.Entities()
            qualify Ntile(3) over (order by Name) = 1";

        // 9 rows split into 3 buckets of 3
        // Sorted: A, B, C, D, E, F, G, H, I
        // Bucket 1: A, B, C
        var sources = CreateSingleSource(
            new BasicEntity("E"),
            new BasicEntity("A"),
            new BasicEntity("I"),
            new BasicEntity("C"),
            new BasicEntity("G"),
            new BasicEntity("B"),
            new BasicEntity("H"),
            new BasicEntity("D"),
            new BasicEntity("F"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "A"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "B"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "C"));
    }

    [TestMethod]
    public void WhenQualifyWithFrameSpec_ShouldFilterOnFramedResult()
    {
        var query = @"
            select Name, Population,
                   Sum(Population) over (order by Name rows between 1 preceding and 1 following) as SlideSum
            from #A.Entities()
            qualify Sum(Population) over (order by Name rows between 1 preceding and 1 following) >= 90";

        // Sorted: A(10), B(20), C(30), D(40), E(50)
        // Sliding sums: A=30, B=60, C=90, D=120, E=90
        // Qualify >= 90: C(90), D(120), E(90)
        var sources = CreateSingleSource(
            new BasicEntity("D") { Population = 40 },
            new BasicEntity("A") { Population = 10 },
            new BasicEntity("E") { Population = 50 },
            new BasicEntity("B") { Population = 20 },
            new BasicEntity("C") { Population = 30 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "C"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "D"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "E"));
    }

    [TestMethod]
    public void WhenQualifyWithWhereAndSkipTake_ShouldApplyInCorrectOrder()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            where Population > 10
            qualify RowNumber() over (order by Name) <= 4
            skip 1
            take 2";

        // Original 6 rows, WHERE removes A(10)
        // After WHERE: B(20), C(30), D(40), E(50), F(60) → rn 1..5
        // QUALIFY <= 4: B, C, D, E (4 rows)
        // SKIP 1 TAKE 2: C, D (2 rows)
        var sources = CreateSingleSource(
            new BasicEntity("D") { Population = 40 },
            new BasicEntity("A") { Population = 10 },
            new BasicEntity("F") { Population = 60 },
            new BasicEntity("B") { Population = 20 },
            new BasicEntity("E") { Population = 50 },
            new BasicEntity("C") { Population = 30 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void WhenQualifyPartitionedWithManyPartitions_ShouldFilterEachIndependently()
    {
        var query = @"
            select Country, City, Name,
                   RowNumber() over (partition by Country, City order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (partition by Country, City order by Name) = 1";

        // 4 partition groups (Country, City), keep first per group
        var sources = CreateSingleSource(
            new BasicEntity("Zach") { Country = "US", City = "NYC" },
            new BasicEntity("Alice") { Country = "US", City = "NYC" },
            new BasicEntity("Bob") { Country = "US", City = "LA" },
            new BasicEntity("Clara") { Country = "UK", City = "London" },
            new BasicEntity("Dan") { Country = "UK", City = "London" },
            new BasicEntity("Eve") { Country = "US", City = "LA" },
            new BasicEntity("Frank") { Country = "UK", City = "Manchester" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // 4 partitions → 4 rows (first alphabetically per partition)
        Assert.AreEqual(4, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[2] == "Alice"));
        Assert.IsTrue(table.Any(r => (string)r.Values[2] == "Bob"));
        Assert.IsTrue(table.Any(r => (string)r.Values[2] == "Clara"));
        Assert.IsTrue(table.Any(r => (string)r.Values[2] == "Frank"));
    }

}
