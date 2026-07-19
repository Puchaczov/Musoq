using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    public void SimpleCteWithUnionTest()
    {
        var query =
            "with p as (select City, Country from #A.entities() union (Country, City) select City, Country from #B.entities()) select City, Country from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "POLAND"],
            ["CZESTOCHOWA", "POLAND"],
            ["KATOWICE", "POLAND"],
            ["BERLIN", "GERMANY"],
            ["MUNICH", "GERMANY"]);
    }

    [TestMethod]
    public void SimpleCteWithUnionAllTest()
    {
        var query =
            "with p as (select City, Country from #A.entities() union all (Country) select City, Country from #B.entities()) select City, Country from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "POLAND"],
            ["CZESTOCHOWA", "POLAND"],
            ["KATOWICE", "POLAND"],
            ["BERLIN", "GERMANY"],
            ["MUNICH", "GERMANY"]);
    }

    [TestMethod]
    public void SimpleCteWithExceptTest()
    {
        var query =
            "with p as (select City, Country from #A.entities() except (Country) select City, Country from #B.entities()) select City, Country from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["HELSINKI", "FINLAND"]);
    }

    [TestMethod]
    public void SimpleCteWithIntersectTest()
    {
        var query =
            "with p as (select City, Country from #A.entities() intersect (Country, City) select City, Country from #B.entities()) select City, Country from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("HELSINKI", "FINLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["HELSINKI", "FINLAND"],
            ["WARSAW", "POLAND"]);
    }

    [TestMethod]
    public void CteWithSetOperatorTest()
    {
        var query = @"
with p as (
    select City, Country from #A.entities() intersect (Country, City)
    select City, Country from #B.entities()
)
select City, Country from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("HELSINKI", "FINLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["HELSINKI", "FINLAND"],
            ["WARSAW", "POLAND"]);
    }

    [TestMethod]
    public void CteWithSetInInnerOuterExpressionTest()
    {
        var query = @"
with p as (
    select City, Country from #A.entities() intersect (Country, City)
    select City, Country from #B.entities()
)
select City, Country from p union (City, Country)
select City, Country from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("HELSINKI", "FINLAND", 500)
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("NEW YORK", "USA", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["HELSINKI", "FINLAND"],
            ["WARSAW", "POLAND"],
            ["NEW YORK", "USA"]);
    }
}
