using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PivotTests : BasicEntityTestBase
{
    [TestMethod]
    public void Pivot_WithSingleMeasureAndGroupBy_ShouldReturnStaticColumns()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Jan' as Jan, 'Feb' as Feb)
                             using Sum(Money) as Sales
                             group by City
                             order by City
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
    public void Pivot_WithMultipleMeasures_ShouldReturnValueAndMeasureAliases()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Jan' as Jan, 'Feb' as Feb)
                             using Sum(Money) as Sales, Count(*) as Orders
                             group by City
                             order by City
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Jan_Sales", typeof(decimal?)),
            ("Jan_Orders", typeof(long)),
            ("Feb_Sales", typeof(decimal?)),
            ("Feb_Orders", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", 5m, 1L, 15m, 1L],
            ["NY", 10m, 1L, 20m, 1L]);
    }

    [TestMethod]
    public void Pivot_WithoutGroupBy_ShouldReturnOneGlobalAggregateRow()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Jan' as Jan, 'Feb' as Feb)
                             using Sum(Money) as Sales
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Jan", typeof(decimal?)),
            ("Feb", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [15m, 35m]);
    }

    [TestMethod]
    public void Pivot_WithOrderSkipTake_ShouldApplyPostAggregateClauses()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Jan' as Jan, 'Feb' as Feb)
                             using Sum(Money) as Sales
                             group by City
                             order by City
                             skip 1
                             take 1
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSourcesWithThreeCities()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Jan", typeof(decimal?)),
            ("Feb", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["NY", 10m, 20m]);
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return CreateSingleSource(
            new BasicEntity { City = "NY", Month = "Jan", Money = 10m },
            new BasicEntity { City = "NY", Month = "Feb", Money = 20m },
            new BasicEntity { City = "LA", Month = "Jan", Money = 5m },
            new BasicEntity { City = "LA", Month = "Feb", Money = 15m });
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSourcesWithThreeCities()
    {
        return CreateSingleSource(
            new BasicEntity { City = "NY", Month = "Jan", Money = 10m },
            new BasicEntity { City = "NY", Month = "Feb", Money = 20m },
            new BasicEntity { City = "LA", Month = "Jan", Money = 5m },
            new BasicEntity { City = "LA", Month = "Feb", Money = 15m },
            new BasicEntity { City = "SF", Month = "Jan", Money = 7m },
            new BasicEntity { City = "SF", Month = "Feb", Money = 1m });
    }
}
