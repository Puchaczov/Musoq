using System.Collections.Generic;
using System.Linq;
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
