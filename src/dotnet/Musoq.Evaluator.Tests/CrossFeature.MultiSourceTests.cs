using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossFeatureMultiSourceTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region QUALIFY + IN Subquery

    [TestMethod]
    public void WhenQualifyWithInSubquery_ShouldFilterBeforeQualify()
    {
        var query = @"
            select a.Name, a.City, RowNumber() over (order by a.Name) as rn
            from #A.entities() a
            where a.City in (select b.City from #B.entities() b)
            qualify RowNumber() over (order by a.Name) <= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC", Population = 100 },
                    new BasicEntity("Bob") { City = "LA", Population = 200 },
                    new BasicEntity("Charlie") { City = "NYC", Population = 300 },
                    new BasicEntity("Diana") { City = "SF", Population = 400 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { City = "NYC" },
                    new BasicEntity("Y") { City = "LA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // IN filters to NYC+LA (Alice, Bob, Charlie), then QUALIFY keeps top 2
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region IN Subquery + Window Function

    [TestMethod]
    public void WhenInSubqueryWithWindowFunction_ShouldWork()
    {
        var query = @"
            select a.Name, a.City, RowNumber() over (partition by a.City order by a.Name) as rn
            from #A.entities() a
            where a.City in (select b.City from #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC", Population = 100 },
                    new BasicEntity("Bob") { City = "LA", Population = 200 },
                    new BasicEntity("Charlie") { City = "NYC", Population = 300 },
                    new BasicEntity("Diana") { City = "SF", Population = 400 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { City = "NYC" },
                    new BasicEntity("Y") { City = "LA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");

        Assert.AreEqual(1, Convert.ToInt32(alice.Values[2]));
        Assert.AreEqual(2, Convert.ToInt32(charlie.Values[2]));
        Assert.AreEqual(1, Convert.ToInt32(bob.Values[2]));
    }

    #endregion

    #region IN Subquery + != Operator

    [TestMethod]
    public void WhenInSubqueryWithNotEquals_ShouldWork()
    {
        var query = @"
            select a.Name, a.City from #A.entities() a
            where a.City in (select b.City from #B.entities() b)
              and a.Name != 'Alice'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC", Population = 100 },
                    new BasicEntity("Bob") { City = "LA", Population = 200 },
                    new BasicEntity("Charlie") { City = "NYC", Population = 300 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { City = "NYC" },
                    new BasicEntity("Y") { City = "LA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsFalse(table.Any(r => (string)r.Values[0] == "Alice"));
    }

    #endregion

    #region ASOF JOIN + Window Function

    [TestMethod]
    public void WhenAsOfJoinWithWindowRowNumber_ShouldWork()
    {
        var query = @"
            select a.Name, a.Population, b.Name as MatchedName, b.Population as MatchedPop,
                   RowNumber() over (order by a.Name) as rn
            from #A.entities() a
            asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Population = 300 },
                    new BasicEntity("Bob") { Population = 200 },
                    new BasicEntity("Charlie") { Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { Population = 150 },
                    new BasicEntity("Y") { Population = 250 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // Alice(300) >= Y(250) closest match; Bob(200) >= X(150) closest; Charlie(100) < 150 → no match
        Assert.AreEqual(2, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(300m, (decimal)alice.Values[1]);
        Assert.AreEqual("Y", (string)alice.Values[2]);
        Assert.AreEqual(250m, (decimal)alice.Values[3]);
        Assert.AreEqual(1, Convert.ToInt32(alice.Values[4]));

        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        Assert.AreEqual(200m, (decimal)bob.Values[1]);
        Assert.AreEqual("X", (string)bob.Values[2]);
        Assert.AreEqual(150m, (decimal)bob.Values[3]);
        Assert.AreEqual(2, Convert.ToInt32(bob.Values[4]));
    }

    #endregion

    #region ASOF JOIN + IN Subquery

    [TestMethod]
    public void WhenAsOfJoinWithInSubquery_ShouldFilterCorrectly()
    {
        var query = @"
            select a.Name, a.Population, b.Name as MatchedName
            from #A.entities() a
            asof join #B.entities() b on a.Population >= b.Population
            where a.Name in (select c.Name from #C.entities() c)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Population = 300 },
                    new BasicEntity("Bob") { Population = 200 },
                    new BasicEntity("Charlie") { Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { Population = 150 },
                    new BasicEntity("Y") { Population = 50 }
                ]
            },
            {
                "#C", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Charlie")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // ASOF matches all 3 A rows (X=150 is closest for Alice/Bob, Y=50 for Charlie).
        // IN filter keeps Alice and Charlie only.
        Assert.AreEqual(2, table.Count);
        foreach (var row in table)
        {
            var name = (string)row.Values[0];
            Assert.IsTrue(name == "Alice" || name == "Charlie", $"Expected Alice or Charlie but got {name}");
        }
    }

    #endregion

    #region ASOF JOIN + QUALIFY

    [TestMethod]
    public void WhenAsOfJoinWithQualify_ShouldWork()
    {
        var query = @"
            select a.Name, a.Population, b.Population as MatchedPop,
                   RowNumber() over (order by a.Name) as rn
            from #A.entities() a
            asof join #B.entities() b on a.Population >= b.Population
            qualify RowNumber() over (order by a.Name) <= 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Population = 300 },
                    new BasicEntity("Bob") { Population = 200 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", (string)table[0].Values[0]);
    }

    #endregion

    #region FILTER + IN Subquery in WHERE

    [TestMethod]
    public void WhenFilterWithInSubqueryInWhere_ShouldWork()
    {
        var query = @"
            select a.Country as Country,
                   a.Count(a.Country) filter (where a.Population > 100) as BigCount,
                   a.Count(a.Country) as AllCount
            from #A.entities() a
            where a.City in (select b.City from #B.entities() b)
            group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("W", "PL", 500) { City = "WARSAW" },
                    new BasicEntity("B", "DE", 50) { City = "BERLIN" },
                    new BasicEntity("P", "FR", 300) { City = "PARIS" },
                    new BasicEntity("L", "UK", 200) { City = "LONDON" }
                ]
            },
            {
                "#B", [
                    new BasicEntity("W", "PL", 500) { City = "WARSAW" },
                    new BasicEntity("B", "DE", 50) { City = "BERLIN" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("BigCount", typeof(long)),
            ("AllCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["PL", 1L, 1L],
            ["DE", 0L, 1L]);
    }

    #endregion

    #region ROWS Frame + IN Subquery

    [TestMethod]
    public void WhenRowsFrameWithInSubqueryInWhere_ShouldWork()
    {
        var query = @"
            select a.Name, a.Population,
                   Sum(a.Population) over (order by a.Name rows between unbounded preceding and current row) as RunSum
            from #A.entities() a
            where a.City in (select b.City from #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC", Population = 100 },
                    new BasicEntity("Bob") { City = "LA", Population = 200 },
                    new BasicEntity("Charlie") { City = "NYC", Population = 300 },
                    new BasicEntity("Diana") { City = "SF", Population = 400 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { City = "NYC" },
                    new BasicEntity("Y") { City = "LA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");

        Assert.AreEqual(100m, Convert.ToDecimal(alice.Values[2]));
        Assert.AreEqual(300m, Convert.ToDecimal(bob.Values[2]));
        Assert.AreEqual(600m, Convert.ToDecimal(charlie.Values[2]));
    }

    #endregion

    #region ROWS Frame + ASOF JOIN

    [TestMethod]
    public void WhenRowsFrameWithAsOfJoin_ShouldWork()
    {
        var query = @"
            select a.Name, a.Population,
                   Sum(a.Population) over (order by a.Name rows between unbounded preceding and current row) as RunSum
            from #A.entities() a
            asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Population = 300 },
                    new BasicEntity("Bob") { Population = 200 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // Both Alice(300) and Bob(200) >= X(150). Order by Name: Alice first.
        Assert.AreEqual(2, table.Count);

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(300m, Convert.ToDecimal(alice.Values[2]));

        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        Assert.AreEqual(500m, Convert.ToDecimal(bob.Values[2]));
    }

    #endregion

    #region AggregateValues + FILTER

    [TestMethod]
    public void WhenAggregateValuesWithFilter_ShouldWork()
    {
        var query = @"
            select a.Country,
                   AggregateValues(a.Name, ', ') as AllNames,
                   Count(a.Name) filter (where a.Population > 200) as BigCityCount
            from #A.entities() a
            group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Country = "PL", Population = 100 },
                    new BasicEntity("Bob") { Country = "PL", Population = 300 },
                    new BasicEntity("Charlie") { Country = "DE", Population = 500 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var pl = table.Single(r => (string)r.Values[0] == "PL");
        Assert.AreEqual(1, Convert.ToInt32(pl.Values[2]));

        var de = table.Single(r => (string)r.Values[0] == "DE");
        Assert.AreEqual(1, Convert.ToInt32(de.Values[2]));
    }

    #endregion

    #region AggregateValues in CTE + outer QUALIFY

    [TestMethod]
    public void WhenAggregateValuesInCteWithOuterQualify_ShouldWork()
    {
        var query = @"
            with grouped as (
                select Country, AggregateValues(Name, ', ') as Names, Count(Name) as Cnt
                from #A.entities()
                group by Country
            )
            select Country, Names, Cnt,
                   RowNumber() over (order by Country) as rn
            from grouped
            qualify RowNumber() over (order by Country) <= 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Country = "PL", Population = 100 },
            new BasicEntity("Bob") { Country = "PL", Population = 300 },
            new BasicEntity("Charlie") { Country = "DE", Population = 500 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region ASOF JOIN + GROUP BY + FILTER

    [TestMethod]
    public void WhenAsOfJoinWithGroupByAndFilter_ShouldWork()
    {
        var query = @"
            select a.Country as Country,
                   Count(a.Name) as Total,
                   Count(a.Name) filter (where a.Population > 200) as BigCount
            from #A.entities() a
            asof join #B.entities() b on a.Population >= b.Population
            group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Country = "PL", Population = 300 },
                    new BasicEntity("Bob") { Country = "PL", Population = 100 },
                    new BasicEntity("Charlie") { Country = "DE", Population = 500 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // All 3 A rows match X(50). GROUP BY Country: PL(Alice+Bob), DE(Charlie)
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Total", typeof(long)),
            ("BigCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["PL", 2L, 1L],
            ["DE", 1L, 1L]);
    }

    #endregion

    #region Chained ASOF JOINs

    [TestMethod]
    public void WhenTwoAsOfJoins_ShouldChainCorrectly()
    {
        var query = @"
            select a.Name, a.Population, b.Name as Match1, c.Name as Match2
            from #A.entities() a
            asof join #B.entities() b on a.Population >= b.Population
            asof join #C.entities() c on a.Population >= c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Population = 500 },
                    new BasicEntity("Bob") { Population = 300 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { Population = 200 },
                    new BasicEntity("Y") { Population = 400 }
                ]
            },
            {
                "#C", [
                    new BasicEntity("P") { Population = 100 },
                    new BasicEntity("Q") { Population = 250 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // Chained ASOF JOINs: each A row yields exactly one closest match in B and one in C
        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Alice"), "Alice should appear");
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Bob"), "Bob should appear");

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual("Y", (string)alice.Values[2], "Alice (500) should match B=Y (400, closest <=500)");
        Assert.AreEqual("Q", (string)alice.Values[3], "Alice (500) should match C=Q (250, closest <=500)");

        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        Assert.AreEqual("X", (string)bob.Values[2], "Bob (300) should match B=X (200, closest <=300)");
        Assert.AreEqual("Q", (string)bob.Values[3], "Bob (300) should match C=Q (250, closest <=300)");
    }

    #endregion

    #region ASOF JOIN + Star Modifier

    [TestMethod]
    [Description("ASOF JOIN + star exclude with alias prefix should work")]
    public void WhenAsOfJoinWithStarExclude_ShouldWork()
    {
        var query = @"
            select a.* exclude (a.City, a.Country), b.Name as MatchedName
            from #A.entities() a
            asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Population = 300, City = "NYC", Country = "US" },
                    new BasicEntity("Bob") { Population = 200, City = "LA", Country = "US" }
                ]
            },
            {
                "#B", [
                    new BasicEntity("X") { Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // Both Alice(300) and Bob(200) >= X(150). Star excludes City and Country.
        Assert.AreEqual(2, table.Count);

        var columnNames = table.Columns.Select(c => c.ColumnName).ToList();
        Assert.IsFalse(columnNames.Any(c => c.Contains("City")), "City should be excluded");
        Assert.IsFalse(columnNames.Any(c => c.Contains("Country")), "Country should be excluded");

        var matchedNameIdx = columnNames.FindIndex(c => c.Contains("MatchedName"));
        Assert.IsTrue(table.All(r => (string)r.Values[matchedNameIdx] == "X"), "Both rows should match X(150)");
    }

    #endregion
}
