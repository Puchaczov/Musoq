using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PivotEdgeTests : BasicEntityTestBase
{
    [TestMethod]
    public void Pivot_WithNullPivotKey_ShouldMatchNullBucket()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in (null as Missing, 'Jan' as Jan)
                             using Count(*) as Orders
                             group by City
                             order by City
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateNullKeySources()).Run();

        Assert.AreEqual(1, table.Count);
        AssertColumn(table, 0, "City", typeof(string));
        AssertColumn(table, 1, "Missing", typeof(long));
        AssertColumn(table, 2, "Jan", typeof(long));
        Assert.AreEqual("NY", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
    }

    [TestMethod]
    public void Pivot_WithDistinctAggregate_ShouldFilterBeforeDistinct()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Jan' as Jan, 'Feb' as Feb)
                             using Count(distinct Name) as Customers
                             group by City
                             order by City
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateDistinctSources()).Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NY", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
    }

    [TestMethod]
    public void Pivot_WithStringNumericAndDateValues_ShouldMatchAllLiteralKinds()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month, Id, Time in (
                                 ('Jan', 1, '2024-01-01') as JanFirst,
                                 ('Feb', 2, '2024-01-02') as FebSecond
                             )
                             using Sum(Money) as Total
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateLiteralKindSources()).Run();

        Assert.AreEqual(1, table.Count);
        AssertColumn(table, 0, "JanFirst", typeof(decimal?));
        AssertColumn(table, 1, "FebSecond", typeof(decimal?));
        Assert.AreEqual(10m, table[0][0]);
        Assert.AreEqual(20m, table[0][1]);
    }

    [TestMethod]
    public void Pivot_WithMultipleMeasures_ShouldPreserveGeneratedColumnOrdering()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Feb' as Feb, 'Jan' as Jan)
                             using Sum(Money) as Sales, Count(*) as Orders
                             group by City
                             order by City
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateOrderingSources()).Run();

        Assert.AreEqual(1, table.Count);
        AssertColumn(table, 0, "City", typeof(string));
        AssertColumn(table, 1, "Feb_Sales", typeof(decimal?));
        AssertColumn(table, 2, "Feb_Orders", typeof(long));
        AssertColumn(table, 3, "Jan_Sales", typeof(decimal?));
        AssertColumn(table, 4, "Jan_Orders", typeof(long));
        Assert.AreEqual("NY", table[0][0]);
        Assert.AreEqual(20m, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual(10m, table[0][3]);
        Assert.AreEqual(1L, table[0][4]);
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateNullKeySources()
    {
        return CreateSingleSource(
            new BasicEntity { City = "NY", Month = null },
            new BasicEntity { City = "NY", Month = "Jan" });
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateDistinctSources()
    {
        return CreateSingleSource(
            new BasicEntity { City = "NY", Month = "Jan", Name = "Alice" },
            new BasicEntity { City = "NY", Month = "Jan", Name = "Alice" },
            new BasicEntity { City = "NY", Month = "Jan", Name = "Bob" },
            new BasicEntity { City = "NY", Month = "Feb", Name = "Alice" });
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateLiteralKindSources()
    {
        return CreateSingleSource(
            new BasicEntity { Month = "Jan", Id = 1, Time = new DateTime(2024, 1, 1), Money = 10m },
            new BasicEntity { Month = "Feb", Id = 2, Time = new DateTime(2024, 1, 2), Money = 20m },
            new BasicEntity { Month = "Jan", Id = 2, Time = new DateTime(2024, 1, 1), Money = 99m });
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateOrderingSources()
    {
        return CreateSingleSource(
            new BasicEntity { City = "NY", Month = "Jan", Money = 10m },
            new BasicEntity { City = "NY", Month = "Feb", Money = 20m });
    }
}
