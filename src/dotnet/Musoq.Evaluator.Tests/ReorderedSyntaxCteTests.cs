using System.Collections.Generic;
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"], ["BERLIN", "GERMANY"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW", "POLAND"], ["CZESTOCHOWA", "POLAND"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("TotalPop", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["POLAND", 900m], ["GERMANY", 250m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)), ("Country", typeof(string)), ("OtherPop", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND", 300m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW", 500m], ["CZESTOCHOWA", 400m], ["KATOWICE", 250m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["CZESTOCHOWA", 400m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"], ["BERLIN", "GERMANY"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW", "POLAND"], ["CZESTOCHOWA", "POLAND"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("TotalPop", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["POLAND", 900m], ["GERMANY", 250m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["KATOWICE", 250m], ["CZESTOCHOWA", 400m], ["WARSAW", 500m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW", 500m], ["CZESTOCHOWA", 400m]);
    }

}
