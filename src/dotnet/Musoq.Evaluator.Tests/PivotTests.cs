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

        Assert.AreEqual(2, table.Count);
        AssertColumn(table, 0, "City", typeof(string));
        AssertColumn(table, 1, "Jan", typeof(decimal?));
        AssertColumn(table, 2, "Feb", typeof(decimal?));
        Assert.AreEqual("LA", table[0][0]);
        Assert.AreEqual(5m, table[0][1]);
        Assert.AreEqual(15m, table[0][2]);
        Assert.AreEqual("NY", table[1][0]);
        Assert.AreEqual(10m, table[1][1]);
        Assert.AreEqual(20m, table[1][2]);
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

        Assert.AreEqual(2, table.Count);
        AssertColumn(table, 1, "Jan_Sales", typeof(decimal?));
        AssertColumn(table, 2, "Jan_Orders", typeof(long));
        AssertColumn(table, 3, "Feb_Sales", typeof(decimal?));
        AssertColumn(table, 4, "Feb_Orders", typeof(long));
        Assert.AreEqual(5m, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual(15m, table[0][3]);
        Assert.AreEqual(1L, table[0][4]);
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

        Assert.AreEqual(1, table.Count);
        AssertColumn(table, 0, "Jan", typeof(decimal?));
        AssertColumn(table, 1, "Feb", typeof(decimal?));
        Assert.AreEqual(15m, table[0][0]);
        Assert.AreEqual(35m, table[0][1]);
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NY", table[0][0]);
        Assert.AreEqual(10m, table[0][1]);
        Assert.AreEqual(20m, table[0][2]);
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
