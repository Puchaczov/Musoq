using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    public void CteWithTwoOuterExpressionTest()
    {
        var query = @"
with p as (
    select City, Country from #A.entities()
)
select City, Country from p union (City, Country)
select City, Country from #B.entities()";

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
            ["WARSAW", "POLAND"],
            ["CZESTOCHOWA", "POLAND"],
            ["BERLIN", "GERMANY"],
            ["MUNICH", "GERMANY"]);
    }

    [TestMethod]
    public void SimpleCteWithMultipleOuterExpressionsTest()
    {
        var query = @"
with p as (
    select City, Country from #A.entities() intersect (Country, City)
    select City, Country from #B.entities()
) select City, Country from p where Country = 'FINLAND' union (Country, City)
  select City, Country from p where Country = 'POLAND'";

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
                    new BasicEntity("TOKYO", "JAPAN", 500),
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
    public void MultipleCteExpressionsTest()
    {
        const string query = @"
with p as (
    select City, Country from #A.entities()
), c as (
    select City, Country from #B.entities()
), d as (
    select City, Country from p where City = 'HELSINKI'
), f as (
    select City, Country from #B.entities() where City = 'WARSAW'
)
select City, Country from p union (City, Country)
select City, Country from c union (City, Country)
select City, Country from d union (City, Country)
select City, Country from f";

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
            ["WARSAW", "POLAND"],
            ["CZESTOCHOWA", "POLAND"],
            ["BERLIN", "GERMANY"],
            ["MUNICH", "GERMANY"]);
    }
}
