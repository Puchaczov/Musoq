using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests to reproduce and verify DISTINCT + ORDER BY ordering issues.
///     These tests verify that rows are returned in the correct order,
///     not just that the correct set of values is returned.
/// </summary>
[TestClass]
public partial class DistinctOrderByBugTests : BasicEntityTestBase
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


        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { null },
            new object?[] { "Germany" },
            new object?[] { "Poland" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { 500m },
            new object?[] { 300m },
            new object?[] { 200m },
            new object?[] { 100m });
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


        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Poland" },
            new object?[] { "Germany" },
            new object?[] { "France" });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("Country", typeof(string)),
            ("Count(Country)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "France", 1L },
            new object?[] { "Germany", 2L },
            new object?[] { "Poland", 2L });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("Country", typeof(string)),
            ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "France", "Paris" },
            new object?[] { "Germany", "Munich" },
            new object?[] { "Germany", "Berlin" },
            new object?[] { "Poland", "Warsaw" },
            new object?[] { "Poland", "Krakow" });
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


        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Germany" },
            new object?[] { "France" });
    }

    #endregion
}
