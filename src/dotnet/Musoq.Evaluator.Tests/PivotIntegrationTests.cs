using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PivotIntegrationTests : BasicEntityTestBase
{
    [TestMethod]
    public void Pivot_InCte_ShouldBeSelectable()
    {
        const string query = """
                             with p as (
                                 pivot #A.Entities()
                                 on Month in ('Jan' as Jan, 'Feb' as Feb)
                                 using Sum(Money) as Sales
                                 group by City
                             )
                             select City, Jan, Feb from p order by City
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Jan", typeof(decimal?)),
            ("Feb", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", 5m, 15m],
            ["NY", 10m, 20m]);
    }

    [TestMethod]
    public void Pivot_InDerivedTable_ShouldBeSelectable()
    {
        const string query = """
                             select p.City, p.Jan
                             from (
                                 pivot #A.Entities()
                                 on Month in ('Jan' as Jan, 'Feb' as Feb)
                                 using Sum(Money) as Sales
                                 group by City
                             ) p
                             order by p.City
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.City", typeof(string)),
            ("p.Jan", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", 5m],
            ["NY", 10m]);
    }

    [TestMethod]
    public void Pivot_InCteJoinedWithSource_ShouldComposeWithJoins()
    {
        const string query = """
                             with p as (
                                 pivot #A.Entities()
                                 on Month in ('Jan' as Jan, 'Feb' as Feb)
                                 using Sum(Money) as Sales
                                 group by City
                             )
                             select p.City, p.Jan, c.Country
                             from p
                             inner join #B.Entities() c on p.City = c.City
                             order by p.City
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSourcesWithLookup()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.City", typeof(string)),
            ("p.Jan", typeof(decimal?)),
            ("c.Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", 5m, "USA-West"],
            ["NY", 10m, "USA-East"]);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { City = "NY", Month = "Jan", Money = 10m },
                new BasicEntity { City = "NY", Month = "Feb", Money = 20m },
                new BasicEntity { City = "LA", Month = "Jan", Money = 5m },
                new BasicEntity { City = "LA", Month = "Feb", Money = 15m }
            ]
        };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSourcesWithLookup()
    {
        var sources = CreateSources();
        sources["#B"] =
        [
            new BasicEntity { City = "NY", Country = "USA-East" },
            new BasicEntity { City = "LA", Country = "USA-West" }
        ];
        return sources;
    }
}
