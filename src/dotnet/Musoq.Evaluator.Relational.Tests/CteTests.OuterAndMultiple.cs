using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    public void CteWithTwoOuterExpressionTest()
    {
        var query = (
            "\n"
            + "with p as (\n"
            + "    select City, Country from #A.entities()\n"
            + ")\n"
            + "select City, Country from p union (City, Country)\n"
            + "select City, Country from #B.entities()"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                ]
            },
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
            ["MUNICH", "GERMANY"]
        );
    }

    [TestMethod]
    public void SimpleCteWithMultipleOuterExpressionsTest()
    {
        var query = (
            "\n"
            + "with p as (\n"
            + "    select City, Country from #A.entities() intersect (Country, City)\n"
            + "    select City, Country from #B.entities()\n"
            + ") select City, Country from p where Country = 'FINLAND' union (Country, City)\n"
            + "  select City, Country from p where Country = 'POLAND'"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("TOKYO", "JAPAN", 500),
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["HELSINKI", "FINLAND"], ["WARSAW", "POLAND"]);
    }

    [TestMethod]
    public void MultipleCteExpressionsTest()
    {
        const string query = (
            "\n"
            + "with p as (\n"
            + "    select City, Country from #A.entities()\n"
            + "), c as (\n"
            + "    select City, Country from #B.entities()\n"
            + "), d as (\n"
            + "    select City, Country from p where City = 'HELSINKI'\n"
            + "), f as (\n"
            + "    select City, Country from #B.entities() where City = 'WARSAW'\n"
            + ")\n"
            + "select City, Country from p union (City, Country)\n"
            + "select City, Country from c union (City, Country)\n"
            + "select City, Country from d union (City, Country)\n"
            + "select City, Country from f"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("HELSINKI", "FINLAND", 500),
                ]
            },
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
            ["MUNICH", "GERMANY"]
        );
    }
}
