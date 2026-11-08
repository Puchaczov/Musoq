using System.Collections.Generic;
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["POLAND", "WARSAW"],
            ["POLAND", "CZESTOCHOWA"],
            ["POLAND", "KATOWICE"],
            ["GERMANY", "BERLIN"],
            ["GERMANY", "MUNICH"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW", "POLAND"],
            ["CZESTOCHOWA", "POLAND"],
            ["KATOWICE", "POLAND"],
            ["BERLIN", "GERMANY"],
            ["MUNICH", "GERMANY"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["POLAND", 1150m],
            ["GERMANY", 600m]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["POLAND", 1150m],
            ["GERMANY", 600m]);
    }
}
