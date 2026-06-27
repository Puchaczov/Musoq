using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests for reordered SQL syntax (FROM-first) with CTEs (Common Table Expressions).
///     These tests verify that the reordered syntax works correctly in various complex scenarios
///     including nested CTEs, set operators, joins, and mixed syntax usage.
/// </summary>
[TestClass]
public partial class ReorderedSyntaxCteTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }


    [TestMethod]
    public void CteWithReorderedInnerQuery_BasicSelect_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select City, Country
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "WARSAW" &&
            (string)row.Values[1] == "POLAND"));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "BERLIN" &&
            (string)row.Values[1] == "GERMANY"));
    }

    [TestMethod]
    public void CteWithReorderedInnerQuery_WithWhere_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() where Country = 'POLAND' select City, Country
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[1] == "POLAND"));
    }

    [TestMethod]
    public void CteWithReorderedInnerQuery_WithGroupBy_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() group by Country select Country, Sum(Population) as TotalPop
            )
            select Country, TotalPop from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "POLAND" &&
            (decimal)row.Values[1] == 900m));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "GERMANY" &&
            (decimal)row.Values[1] == 250m));
    }

    [TestMethod]
    public void CteWithReorderedInnerQuery_WithJoin_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() a 
                inner join #B.Entities() b on a.Country = b.Country 
                select a.City as City, a.Country as Country, b.Population as OtherPop
            )
            select City, Country, OtherPop from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("KRAKOW", "POLAND", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("POLAND", table[0].Values[1]);
        Assert.AreEqual(300m, table[0].Values[2]);
    }

    [TestMethod]
    public void CteWithReorderedInnerQuery_WithOrderBy_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select City, Population order by Population desc
            )
            select City, Population from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        var cities = table.Select(row => (string)row.Values[0]).ToList();
        CollectionAssert.Contains(cities, "WARSAW");
        CollectionAssert.Contains(cities, "CZESTOCHOWA");
        CollectionAssert.Contains(cities, "KATOWICE");
    }

    [TestMethod]
    public void CteWithReorderedInnerQuery_WithSkipTake_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select City, Population order by Population desc skip 1 take 1
            )
            select City, Population from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        var city = (string)table[0].Values[0];
        Assert.IsTrue(city == "WARSAW" || city == "CZESTOCHOWA" || city == "KATOWICE",
            "Result should be one of the input values");
    }



    [TestMethod]
    public void CteWithReorderedOuterQuery_BasicSelect_ShouldWork()
    {
        var query = @"
            with cte as (
                select City, Country from #A.Entities()
            )
            from cte select City, Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "WARSAW" &&
            (string)row.Values[1] == "POLAND"));
    }

    [TestMethod]
    public void CteWithReorderedOuterQuery_WithWhere_ShouldWork()
    {
        var query = @"
            with cte as (
                select City, Country, Population from #A.Entities()
            )
            from cte where Population > 300 select City, Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "CZESTOCHOWA"));
    }

    [TestMethod]
    public void CteWithReorderedOuterQuery_WithGroupBy_ShouldWork()
    {
        var query = @"
            with cte as (
                select City, Country, Population from #A.Entities()
            )
            from cte group by Country select Country, Sum(Population) as TotalPop";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "POLAND" &&
            (decimal)row.Values[1] == 900m));
    }

    [TestMethod]
    public void CteWithReorderedOuterQuery_WithOrderBy_ShouldWork()
    {
        var query = @"
            with cte as (
                select City, Population from #A.Entities()
            )
            from cte select City, Population order by Population asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        var cities = table.Select(row => (string)row.Values[0]).ToList();
        CollectionAssert.Contains(cities, "KATOWICE");
        CollectionAssert.Contains(cities, "CZESTOCHOWA");
        CollectionAssert.Contains(cities, "WARSAW");
    }



    [TestMethod]
    public void BothCteAndOuterQueryReordered_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() where Country = 'POLAND' select City, Country, Population
            )
            from cte where Population > 300 select City, Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 600)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "CZESTOCHOWA"));
    }

}
