using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class DistinctOrderByBugTests
{
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

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Austria" }, new object?[] { "France" },
            new object?[] { "Germany" }, new object?[] { "Poland" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Poland" }, new object?[] { "Germany" },
            new object?[] { "France" }, new object?[] { "Austria" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Poland" }, new object?[] { "Germany" },
            new object?[] { "France" }, new object?[] { "Austria" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Austria" }, new object?[] { "France" },
            new object?[] { "Germany" }, new object?[] { "Poland" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Apple" }, new object?[] { "Banana" },
            new object?[] { "Cherry" }, new object?[] { "Date" },
            new object?[] { "Elderberry" }, new object?[] { "Fig" },
            new object?[] { "Grape" }, new object?[] { "Honeydew" },
            new object?[] { "Kiwi" }, new object?[] { "Lemon" },
            new object?[] { "Mango" }, new object?[] { "Nectarine" },
            new object?[] { "Orange" }, new object?[] { "Papaya" },
            new object?[] { "Quince" }, new object?[] { "Raspberry" },
            new object?[] { "Zebra" });
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Zebra" }, new object?[] { "Raspberry" },
            new object?[] { "Quince" }, new object?[] { "Papaya" },
            new object?[] { "Orange" }, new object?[] { "Nectarine" },
            new object?[] { "Mango" }, new object?[] { "Lemon" },
            new object?[] { "Kiwi" }, new object?[] { "Honeydew" },
            new object?[] { "Grape" }, new object?[] { "Fig" },
            new object?[] { "Elderberry" }, new object?[] { "Date" },
            new object?[] { "Cherry" }, new object?[] { "Banana" },
            new object?[] { "Apple" });
    }

    #endregion
}
