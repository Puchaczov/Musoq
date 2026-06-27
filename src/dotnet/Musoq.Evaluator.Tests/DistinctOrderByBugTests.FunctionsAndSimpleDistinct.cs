using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class DistinctOrderByBugTests
{
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

}
