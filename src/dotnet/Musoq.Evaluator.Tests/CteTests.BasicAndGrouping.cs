using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    public void SimpleCteTest()
    {
        var query = "with p as (select City, Country from #A.entities()) select Country, City from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("City", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row =>
                          (string)row.Values[0] == "POLAND") == 3 &&
                      table.Any(row =>
                          (string)row.Values[0] == "POLAND" &&
                          (string)row.Values[1] == "WARSAW") &&
                      table.Any(row =>
                          (string)row.Values[0] == "POLAND" &&
                          (string)row.Values[1] == "CZESTOCHOWA") &&
                      table.Any(row =>
                          (string)row.Values[0] == "POLAND" &&
                          (string)row.Values[1] == "KATOWICE"),
            "Expected three rows for POLAND with cities WARSAW, CZESTOCHOWA and KATOWICE");

        Assert.IsTrue(table.Count(row =>
                          (string)row.Values[0] == "GERMANY") == 2 &&
                      table.Any(row =>
                          (string)row.Values[0] == "GERMANY" &&
                          (string)row.Values[1] == "BERLIN") &&
                      table.Any(row =>
                          (string)row.Values[0] == "GERMANY" &&
                          (string)row.Values[1] == "MUNICH"),
            "Expected two rows for GERMANY with cities BERLIN and MUNICH");
    }

    [TestMethod]
    public void SimpleCteWithStarTest()
    {
        var query = "with p as (select City, Country from #A.entities()) select * from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
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
            (string)entry.Values[1] == "POLAND"
        ), "First entry should be WARSAW, POLAND");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "CZESTOCHOWA" &&
            (string)entry.Values[1] == "POLAND"
        ), "Second entry should be CZESTOCHOWA, POLAND");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "KATOWICE" &&
            (string)entry.Values[1] == "POLAND"
        ), "Third entry should be KATOWICE, POLAND");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "BERLIN" &&
            (string)entry.Values[1] == "GERMANY"
        ), "Fourth entry should be BERLIN, GERMANY");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "MUNICH" &&
            (string)entry.Values[1] == "GERMANY"
        ), "Fifth entry should be MUNICH, GERMANY");
    }

    [TestMethod]
    public void SimpleCteWithGroupingTest()
    {
        var query =
            "with p as (select Country, Sum(Population) from #A.entities() group by Country) select * from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "POLAND" &&
                (decimal)row.Values[1] == 1150m),
            "Expected row for POLAND with value 1150");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "GERMANY" &&
                (decimal)row.Values[1] == 600m),
            "Expected row for GERMANY with value 600");
    }

    [TestMethod]
    public void WhenSameAliasesUsedWithinCteInnerExpression_ShouldThrow()
    {
        var query =
            "with p as (select 1 from #A.entities() a inner join #A.entities() a on 1 = 1) select * from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3021_DuplicateAlias, DiagnosticPhase.Bind);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void SimpleCteWithGrouping2Test()
    {
        var query =
            @"
with p as (
    select
        Population,
        Country
    from #A.entities()
)
select Country, Sum(Population) from p group by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).ColumnName);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "POLAND" &&
            (decimal)entry.Values[1] == 1150m
        ), "First entry should be POLAND, 1150");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "GERMANY" &&
            (decimal)entry.Values[1] == 600m
        ), "Second entry should be GERMANY, 600");
    }
}
