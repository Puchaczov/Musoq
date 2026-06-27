using System.Collections.Generic;
using System.Linq;
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        Assert.AreEqual(3,
            table.Count(row =>
                (string)row.Values[1] == "POLAND" &&
                new[] { "WARSAW", "CZESTOCHOWA", "KATOWICE" }.Contains((string)row.Values[0])),
            "Expected 3 cities from Poland not found");

        Assert.AreEqual(2,
            table.Count(row =>
                (string)row.Values[1] == "GERMANY" &&
                new[] { "BERLIN", "MUNICH" }.Contains((string)row.Values[0])),
            "Expected 2 cities from Germany not found");
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(5, table.Count, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "WARSAW" &&
                (string)entry.Values[1] == "POLAND"),
            "Entry for WARSAW, POLAND should match");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "CZESTOCHOWA" &&
                (string)entry.Values[1] == "POLAND"),
            "Entry for CZESTOCHOWA, POLAND should match");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "KATOWICE" &&
                (string)entry.Values[1] == "POLAND"),
            "Entry for KATOWICE, POLAND should match");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "BERLIN" &&
                (string)entry.Values[1] == "GERMANY"),
            "Entry for BERLIN, GERMANY should match");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "MUNICH" &&
                (string)entry.Values[1] == "GERMANY"),
            "Entry for MUNICH, GERMANY should match");
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(1, table.Count, "Table should have 1 entry");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "HELSINKI" &&
            (string)entry.Values[1] == "FINLAND"
        ), "First entry should be HELSINKI, FINLAND");
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.All(row =>
                new[] { ("HELSINKI", "FINLAND"), ("WARSAW", "POLAND") }.Contains(((string)row.Values[0],
                    (string)row.Values[1]))),
            "Expected rows with values: (HELSINKI,FINLAND), (WARSAW,POLAND)");
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "HELSINKI" &&
                (string)row.Values[1] == "FINLAND"),
            "Expected row for HELSINKI, FINLAND");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "WARSAW" &&
                (string)row.Values[1] == "POLAND"),
            "Expected row for WARSAW, POLAND");
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

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "HELSINKI" &&
                (string)entry.Values[1] == "FINLAND"),
            "First entry should be Helsinki, Finland");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "WARSAW" &&
                (string)entry.Values[1] == "POLAND"),
            "Second entry should be Warsaw, Poland");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "NEW YORK" &&
                (string)entry.Values[1] == "USA"),
            "Third entry should be New York, USA");
    }
}
