using System.Collections.Generic;
using System.Linq;
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "HELSINKI" &&
                (string)row.Values[1] == "FINLAND"),
            "Missing HELSINKI/FINLAND");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "WARSAW" &&
                (string)row.Values[1] == "POLAND"),
            "Missing WARSAW/POLAND");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "CZESTOCHOWA" &&
                (string)row.Values[1] == "POLAND"),
            "Missing CZESTOCHOWA/POLAND");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "BERLIN" &&
                (string)row.Values[1] == "GERMANY"),
            "Missing BERLIN/GERMANY");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "MUNICH" &&
                (string)row.Values[1] == "GERMANY"),
            "Missing MUNICH/GERMANY");
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "HELSINKI" &&
            (string)entry.Values[1] == "FINLAND"
        ), "First entry should be HELSINKI, FINLAND");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "WARSAW" &&
            (string)entry.Values[1] == "POLAND"
        ), "Second entry should be WARSAW, POLAND");
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "HELSINKI" &&
                (string)row.Values[1] == "FINLAND"),
            "Expected row for HELSINKI, FINLAND");

        Assert.IsTrue(table.Count(row =>
                          (string)row.Values[1] == "POLAND") == 2 &&
                      table.Any(row =>
                          (string)row.Values[0] == "WARSAW" &&
                          (string)row.Values[1] == "POLAND") &&
                      table.Any(row =>
                          (string)row.Values[0] == "CZESTOCHOWA" &&
                          (string)row.Values[1] == "POLAND"),
            "Expected two rows for POLAND with cities WARSAW and CZESTOCHOWA");

        Assert.IsTrue(table.Count(row =>
                          (string)row.Values[1] == "GERMANY") == 2 &&
                      table.Any(row =>
                          (string)row.Values[0] == "BERLIN" &&
                          (string)row.Values[1] == "GERMANY") &&
                      table.Any(row =>
                          (string)row.Values[0] == "MUNICH" &&
                          (string)row.Values[1] == "GERMANY"),
            "Expected two rows for GERMANY with cities BERLIN and MUNICH");
    }
}
