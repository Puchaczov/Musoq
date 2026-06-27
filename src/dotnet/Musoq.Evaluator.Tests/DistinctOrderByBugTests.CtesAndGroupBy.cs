using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class DistinctOrderByBugTests
{

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

}
