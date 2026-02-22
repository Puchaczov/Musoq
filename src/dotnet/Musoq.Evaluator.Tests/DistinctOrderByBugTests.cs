using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests to reproduce and verify DISTINCT + ORDER BY ordering issues.
///     These tests verify that rows are returned in the correct order,
///     not just that the correct set of values is returned.
/// </summary>
[TestClass]
public class DistinctOrderByBugTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region Edge case: DISTINCT + ORDER BY with NULL values

    [TestMethod]
    public void WhenDistinctWithNullValues_OrderByAsc_NullsSortFirst()
    {
        var query = "select distinct Country from #A.Entities() order by Country asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c1") { Country = null },
                    new BasicEntity("c2") { Country = "Poland" },
                    new BasicEntity("c3") { Country = null },
                    new BasicEntity("c4") { Country = "Germany" },
                    new BasicEntity("c5") { Country = "Poland" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Count);
        Assert.IsNull(table[0].Values[0]);
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual("Poland", table[2].Values[0]);
    }

    #endregion

    #region Edge case: DISTINCT + ORDER BY on numeric column

    [TestMethod]
    public void WhenDistinctNumericWithOrderByDesc_ShouldSortNumerically()
    {
        var query = "select distinct Population from #A.Entities() order by Population desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c1") { Population = 100m },
                    new BasicEntity("c2") { Population = 500m },
                    new BasicEntity("c3") { Population = 100m },
                    new BasicEntity("c4") { Population = 300m },
                    new BasicEntity("c5") { Population = 500m },
                    new BasicEntity("c6") { Population = 200m }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual(500m, table[0].Values[0]);
        Assert.AreEqual(300m, table[1].Values[0]);
        Assert.AreEqual(200m, table[2].Values[0]);
        Assert.AreEqual(100m, table[3].Values[0]);
    }

    #endregion

    #region Edge case: DISTINCT + ORDER BY with WHERE clause

    [TestMethod]
    public void WhenDistinctWithWhereAndOrderByDesc_ShouldFilterThenDistinctThenSort()
    {
        var query = "select distinct Country from #A.Entities() where Population > 150 order by Country desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c1", "Poland", 500),
                    new BasicEntity("c2", "Germany", 100),
                    new BasicEntity("c3", "Poland", 200),
                    new BasicEntity("c4", "France", 300),
                    new BasicEntity("c5", "Germany", 250),
                    new BasicEntity("c6", "Austria", 50)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Poland", table[0].Values[0]);
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual("France", table[2].Values[0]);
    }

    #endregion

    #region Edge case: DISTINCT + aggregate + ORDER BY

    [TestMethod]
    public void WhenDistinctWithCountAndOrderBy_ShouldWork()
    {
        var query = "select distinct Country, Count(Country) from #A.Entities() group by Country order by Country asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c1", "Poland", 500),
                    new BasicEntity("c2", "Germany", 200),
                    new BasicEntity("c3", "Poland", 150),
                    new BasicEntity("c4", "France", 300),
                    new BasicEntity("c5", "Germany", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("France", table[0].Values[0]);
        Assert.AreEqual(1, table[0].Values[1]);
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual(2, table[1].Values[1]);
        Assert.AreEqual("Poland", table[2].Values[0]);
        Assert.AreEqual(2, table[2].Values[1]);
    }

    #endregion

    #region DISTINCT + ORDER BY with multiple columns

    [TestMethod]
    public void WhenDistinctWithMultipleColumnsOrderByFirst_ShouldOrderCorrectly()
    {
        var query = "select distinct Country, City from #A.Entities() order by Country asc, City desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 100),
                    new BasicEntity("Munich", "Germany", 200),
                    new BasicEntity("Paris", "France", 600)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);

        Assert.AreEqual("France", table[0].Values[0]);
        Assert.AreEqual("Paris", table[0].Values[1]);

        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual("Munich", table[1].Values[1]);

        Assert.AreEqual("Germany", table[2].Values[0]);
        Assert.AreEqual("Berlin", table[2].Values[1]);

        Assert.AreEqual("Poland", table[3].Values[0]);
        Assert.AreEqual("Warsaw", table[3].Values[1]);

        Assert.AreEqual("Poland", table[4].Values[0]);
        Assert.AreEqual("Krakow", table[4].Values[1]);
    }

    #endregion

    #region DISTINCT + ORDER BY + SKIP/TAKE

    [TestMethod]
    public void WhenDistinctWithOrderByDescAndSkipTake_ShouldOrderThenPaginate()
    {
        var query = "select distinct Country from #A.Entities() order by Country desc skip 1 take 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c5", "Poland", 500),
                    new BasicEntity("c1", "Germany", 200),
                    new BasicEntity("c3", "Poland", 150),
                    new BasicEntity("c4", "France", 300),
                    new BasicEntity("c2", "Germany", 250),
                    new BasicEntity("c6", "Austria", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Germany", table[0].Values[0]);
        Assert.AreEqual("France", table[1].Values[0]);
    }

    #endregion

    #region Multiple CTEs with DISTINCT and ORDER BY

    [TestMethod]
    public void WhenMultipleCtes_WithDistinct_OuterOrderBy_ShouldOrderCorrectly()
    {
        var query = @"
            with 
                countries as (
                    select distinct Country from #A.Entities()
                ),
                cities as (
                    select distinct City from #A.Entities()
                )
            select Country from countries order by Country desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Berlin", "Germany", 100),
                    new BasicEntity("Paris", "France", 600)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Poland", table[0].Values[0]);
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual("France", table[2].Values[0]);
    }

    #endregion

    #region Edge case: DISTINCT + ORDER BY with explicit GROUP BY having more columns than SELECT

    /// <summary>
    ///     When GROUP BY has more columns than SELECT, the outer query may see
    ///     duplicate values in the SELECT column. DISTINCT must eliminate them
    ///     while ORDER BY preserves the correct sort.
    /// </summary>
    [TestMethod]
    public void WhenDistinctWithExplicitGroupByMoreColumnsThanSelect_OrderBy_ShouldWork()
    {
        var query = "select distinct City from #A.Entities() group by Country, City order by City asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Berlin", "Austria", 200),
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Paris", "Belgium", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Berlin", table[0].Values[0]);
        Assert.AreEqual("Krakow", table[1].Values[0]);
        Assert.AreEqual("Paris", table[2].Values[0]);
        Assert.AreEqual("Warsaw", table[3].Values[0]);
    }

    [TestMethod]
    public void WhenDistinctWithExplicitGroupByMoreColumnsThanSelect_OrderByDesc_ShouldWork()
    {
        var query = "select distinct City from #A.Entities() group by Country, City order by City desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Berlin", "Germany", 350),
                    new BasicEntity("Berlin", "Austria", 200),
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Krakow", "Poland", 400),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Paris", "Belgium", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Warsaw", table[0].Values[0]);
        Assert.AreEqual("Paris", table[1].Values[0]);
        Assert.AreEqual("Krakow", table[2].Values[0]);
        Assert.AreEqual("Berlin", table[3].Values[0]);
    }

    #endregion

    #region Edge case: DISTINCT + ORDER BY with function expression

    [TestMethod]
    public void WhenDistinctWithToUpperAndOrderBy_ShouldWork()
    {
        var query =
            "select distinct ToUpperInvariant(Country) from #A.Entities() order by ToUpperInvariant(Country) asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c1") { Country = "poland" },
                    new BasicEntity("c2") { Country = "Germany" },
                    new BasicEntity("c3") { Country = "POLAND" },
                    new BasicEntity("c4") { Country = "germany" },
                    new BasicEntity("c5") { Country = "France" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("FRANCE", table[0].Values[0]);
        Assert.AreEqual("GERMANY", table[1].Values[0]);
        Assert.AreEqual("POLAND", table[2].Values[0]);
    }

    /// <summary>
    ///     Same test but WITHOUT DISTINCT to check if it's a general GROUP BY + expression ORDER BY issue.
    /// </summary>
    [TestMethod]
    public void WhenGroupByWithExpressionOrderBy_WithoutDistinct_ShouldWork()
    {
        var query =
            "select ToUpperInvariant(Country) from #A.Entities() group by ToUpperInvariant(Country) order by ToUpperInvariant(Country) asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c1") { Country = "poland" },
                    new BasicEntity("c2") { Country = "Germany" },
                    new BasicEntity("c3") { Country = "POLAND" },
                    new BasicEntity("c4") { Country = "germany" },
                    new BasicEntity("c5") { Country = "France" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("FRANCE", table[0].Values[0]);
        Assert.AreEqual("GERMANY", table[1].Values[0]);
        Assert.AreEqual("POLAND", table[2].Values[0]);
    }

    /// <summary>
    ///     Test simple column ORDER BY with explicit GROUP BY - baseline.
    /// </summary>
    [TestMethod]
    public void WhenGroupByColumnWithOrderByColumn_ShouldWork()
    {
        var query = "select Country from #A.Entities() group by Country order by Country asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c1") { Country = "Poland" },
                    new BasicEntity("c2") { Country = "Germany" },
                    new BasicEntity("c3") { Country = "Poland" },
                    new BasicEntity("c4") { Country = "France" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("France", table[0].Values[0]);
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual("Poland", table[2].Values[0]);
    }

    #endregion

    #region Simple DISTINCT + ORDER BY

    [TestMethod]
    public void WhenDistinctWithOrderByAsc_ShouldReturnRowsInAscendingOrder()
    {
        var query = "select distinct Country from #A.Entities() order by Country asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c5", "Poland", 500),
                    new BasicEntity("c1", "Germany", 200),
                    new BasicEntity("c3", "Poland", 150),
                    new BasicEntity("c4", "France", 300),
                    new BasicEntity("c2", "Germany", 250),
                    new BasicEntity("c6", "Austria", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Austria", table[0].Values[0]);
        Assert.AreEqual("France", table[1].Values[0]);
        Assert.AreEqual("Germany", table[2].Values[0]);
        Assert.AreEqual("Poland", table[3].Values[0]);
    }

    [TestMethod]
    public void WhenDistinctWithOrderByDesc_ShouldReturnRowsInDescendingOrder()
    {
        var query = "select distinct Country from #A.Entities() order by Country desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c5", "Poland", 500),
                    new BasicEntity("c1", "Germany", 200),
                    new BasicEntity("c3", "Poland", 150),
                    new BasicEntity("c4", "France", 300),
                    new BasicEntity("c2", "Germany", 250),
                    new BasicEntity("c6", "Austria", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Poland", table[0].Values[0]);
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual("France", table[2].Values[0]);
        Assert.AreEqual("Austria", table[3].Values[0]);
    }

    [TestMethod]
    public void WhenDistinctWithDefaultOrderBy_ShouldReturnRowsInAscendingOrder()
    {
        var query = "select distinct Country from #A.Entities() order by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c5", "Poland", 500),
                    new BasicEntity("c1", "Germany", 200),
                    new BasicEntity("c3", "Poland", 150),
                    new BasicEntity("c4", "France", 300),
                    new BasicEntity("c2", "Germany", 250),
                    new BasicEntity("c6", "Austria", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Austria", table[0].Values[0]);
        Assert.AreEqual("France", table[1].Values[0]);
        Assert.AreEqual("Germany", table[2].Values[0]);
        Assert.AreEqual("Poland", table[3].Values[0]);
    }

    #endregion

    #region CTE with DISTINCT inner + ORDER BY in outer

    [TestMethod]
    public void WhenCteHasDistinct_OuterOrderByAsc_ShouldOrderCorrectly()
    {
        var query = @"
            with cte as (
                select distinct Country from #A.Entities()
            )
            select Country from cte order by Country asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c5", "Poland", 500),
                    new BasicEntity("c1", "Germany", 200),
                    new BasicEntity("c3", "Poland", 150),
                    new BasicEntity("c4", "France", 300),
                    new BasicEntity("c2", "Germany", 250),
                    new BasicEntity("c6", "Austria", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Austria", table[0].Values[0]);
        Assert.AreEqual("France", table[1].Values[0]);
        Assert.AreEqual("Germany", table[2].Values[0]);
        Assert.AreEqual("Poland", table[3].Values[0]);
    }

    [TestMethod]
    public void WhenCteHasDistinct_OuterOrderByDesc_ShouldOrderCorrectly()
    {
        var query = @"
            with cte as (
                select distinct Country from #A.Entities()
            )
            select Country from cte order by Country desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c5", "Poland", 500),
                    new BasicEntity("c1", "Germany", 200),
                    new BasicEntity("c3", "Poland", 150),
                    new BasicEntity("c4", "France", 300),
                    new BasicEntity("c2", "Germany", 250),
                    new BasicEntity("c6", "Austria", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Poland", table[0].Values[0]);
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual("France", table[2].Values[0]);
        Assert.AreEqual("Austria", table[3].Values[0]);
    }

    #endregion

    #region CTE with DISTINCT inner then DISTINCT + ORDER BY outer

    [TestMethod]
    public void WhenCteHasDistinct_OuterDistinctAndOrderByDesc_ShouldOrderCorrectly()
    {
        var query = @"
            with cte as (
                select distinct Country from #A.Entities()
            )
            select distinct Country from cte order by Country desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c5", "Poland", 500),
                    new BasicEntity("c1", "Germany", 200),
                    new BasicEntity("c3", "Poland", 150),
                    new BasicEntity("c4", "France", 300),
                    new BasicEntity("c2", "Germany", 250),
                    new BasicEntity("c6", "Austria", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Poland", table[0].Values[0]);
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual("France", table[2].Values[0]);
        Assert.AreEqual("Austria", table[3].Values[0]);
    }

    [TestMethod]
    public void WhenCteHasDistinct_OuterDistinctAndOrderByAsc_ShouldOrderCorrectly()
    {
        var query = @"
            with cte as (
                select distinct Country from #A.Entities()
            )
            select distinct Country from cte order by Country asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("c5", "Poland", 500),
                    new BasicEntity("c1", "Germany", 200),
                    new BasicEntity("c3", "Poland", 150),
                    new BasicEntity("c4", "France", 300),
                    new BasicEntity("c2", "Germany", 250),
                    new BasicEntity("c6", "Austria", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Austria", table[0].Values[0]);
        Assert.AreEqual("France", table[1].Values[0]);
        Assert.AreEqual("Germany", table[2].Values[0]);
        Assert.AreEqual("Poland", table[3].Values[0]);
    }

    #endregion

    #region Many values to make ordering failures obvious

    [TestMethod]
    public void WhenDistinctWithManyValues_OrderByAsc_ShouldBeStrictlyOrdered()
    {
        var query = "select distinct Name from #A.Entities() order by Name asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Zebra"),
                    new BasicEntity("Mango"),
                    new BasicEntity("Apple"),
                    new BasicEntity("Banana"),
                    new BasicEntity("Zebra"),
                    new BasicEntity("Cherry"),
                    new BasicEntity("Mango"),
                    new BasicEntity("Date"),
                    new BasicEntity("Apple"),
                    new BasicEntity("Fig"),
                    new BasicEntity("Elderberry"),
                    new BasicEntity("Grape"),
                    new BasicEntity("Honeydew"),
                    new BasicEntity("Kiwi"),
                    new BasicEntity("Lemon"),
                    new BasicEntity("Nectarine"),
                    new BasicEntity("Orange"),
                    new BasicEntity("Papaya"),
                    new BasicEntity("Quince"),
                    new BasicEntity("Raspberry")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var names = table.Select(row => (string)row.Values[0]).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();

        Assert.HasCount(sortedNames.Count, names);
        CollectionAssert.AreEqual(sortedNames, names, "Results should be in ascending order");
    }

    [TestMethod]
    public void WhenDistinctWithManyValues_OrderByDesc_ShouldBeStrictlyOrdered()
    {
        var query = "select distinct Name from #A.Entities() order by Name desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Zebra"),
                    new BasicEntity("Mango"),
                    new BasicEntity("Apple"),
                    new BasicEntity("Banana"),
                    new BasicEntity("Zebra"),
                    new BasicEntity("Cherry"),
                    new BasicEntity("Mango"),
                    new BasicEntity("Date"),
                    new BasicEntity("Apple"),
                    new BasicEntity("Fig"),
                    new BasicEntity("Elderberry"),
                    new BasicEntity("Grape"),
                    new BasicEntity("Honeydew"),
                    new BasicEntity("Kiwi"),
                    new BasicEntity("Lemon"),
                    new BasicEntity("Nectarine"),
                    new BasicEntity("Orange"),
                    new BasicEntity("Papaya"),
                    new BasicEntity("Quince"),
                    new BasicEntity("Raspberry")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var names = table.Select(row => (string)row.Values[0]).ToList();
        var sortedNames = names.OrderByDescending(n => n).ToList();

        Assert.HasCount(sortedNames.Count, names);
        CollectionAssert.AreEqual(sortedNames, names, "Results should be in descending order");
    }

    #endregion
}
