using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class QualifyTests
{
    [TestMethod]
    public void WhenQualifyInsideCte_ShouldFilterBeforeOuterQuery()
    {
        var query = @"
            with ranked as (
                select Name, City, RowNumber() over (partition by City order by Name) as rn
                from #A.entities()
                qualify RowNumber() over (partition by City order by Name) <= 1
            )
            select Name, City from ranked";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC" },
                    new BasicEntity("Bob") { City = "NYC" },
                    new BasicEntity("Charlie") { City = "LA" },
                    new BasicEntity("Diana") { City = "LA" },
                    new BasicEntity("Eve") { City = "SF" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // 3 partitions (NYC, LA, SF) → 1 row each → 3 rows
        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Alice" && (string)r.Values[1] == "NYC"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Charlie" && (string)r.Values[1] == "LA"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Eve" && (string)r.Values[1] == "SF"));
    }

    [TestMethod]
    public void WhenQualifyWithInnerJoin_ShouldFilterJoinedResult()
    {
        var query = @"
            select a.Name, b.City, RowNumber() over (partition by b.City order by a.Name) as rn
            from #A.entities() a
            inner join #B.entities() b on a.Id = b.Id
            qualify RowNumber() over (partition by b.City order by a.Name) <= 1";

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

        // 2 partitions (NYC, LA), 1 row each → 2 rows
        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Alice" && (string)r.Values[1] == "NYC"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Bob" && (string)r.Values[1] == "LA"));
    }

    [TestMethod]
    public void WhenQualifyWithOrderBy_ShouldSortAfterFiltering()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (order by Name) <= 3
            order by rn desc";

        var sources = CreateSingleSource(
            new BasicEntity("Eve"),
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Diana"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        // After QUALIFY: Alice(1), Bob(2), Charlie(3)
        // ORDER BY rn DESC: Charlie(3), Bob(2), Alice(1)
        Assert.AreEqual("Charlie", table[0].Values[0]);
        Assert.AreEqual(3L, table[0].Values[1]);
        Assert.AreEqual("Bob", table[1].Values[0]);
        Assert.AreEqual(2L, table[1].Values[1]);
        Assert.AreEqual("Alice", table[2].Values[0]);
        Assert.AreEqual(1L, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenQualifyWithNotEquals_ShouldExcludeMatching()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (order by Name) != 2";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"),
            new BasicEntity("Diana"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // Rows: Alice(1), Bob(2), Charlie(3), Diana(4) → exclude rn=2 → 3 rows
        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Alice"));
        Assert.IsFalse(table.Any(r => (string)r.Values[0] == "Bob"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Charlie"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Diana"));
    }

    [TestMethod]
    public void WhenMultipleWindowFunctionsWithQualifyOnOne_ShouldFilterCorrectly()
    {
        var query = @"
            select Name, City,
                   RowNumber() over (partition by City order by Name) as rn,
                   Count(Name) over (partition by City) as cnt
            from #A.Entities()
            qualify RowNumber() over (partition by City order by Name) <= 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "LA" },
            new BasicEntity("Diana") { City = "LA" },
            new BasicEntity("Eve") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // 2 partitions → 1 row each → 2 rows
        Assert.AreEqual(2, table.Count);

        var nyc = table.Single(r => (string)r.Values[1] == "NYC");
        var la = table.Single(r => (string)r.Values[1] == "LA");

        Assert.AreEqual("Alice", nyc.Values[0]);
        Assert.AreEqual(1L, nyc.Values[2]);
        Assert.AreEqual(2, Convert.ToInt32(nyc.Values[3]));

        Assert.AreEqual("Charlie", la.Values[0]);
        Assert.AreEqual(1L, la.Values[2]);
        Assert.AreEqual(3, Convert.ToInt32(la.Values[3]));
    }

}
