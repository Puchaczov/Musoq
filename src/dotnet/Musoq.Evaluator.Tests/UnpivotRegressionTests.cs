using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class UnpivotRegressionTests : BasicEntityTestBase
{
    [TestMethod]
    public void Run_WhenPivotQueryExecutes_ShouldRemainUnchanged()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Jan' as Jan, 'Feb' as Feb)
                             using Sum(Money) as Sales
                             group by City
                             order by City
                             """;

        var table = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { City = "GDA", Month = "Jan", Money = 10m },
                new BasicEntity { City = "GDA", Month = "Feb", Money = 20m }
            ]
        }).Run();

        Assert.AreEqual(1, table.Count);
        AssertColumn(table, 0, "City", typeof(string));
        AssertColumn(table, 1, "Jan", typeof(decimal?));
        AssertColumn(table, 2, "Feb", typeof(decimal?));
        Assert.AreEqual("GDA", table[0][0]);
        Assert.AreEqual(10m, table[0][1]);
        Assert.AreEqual(20m, table[0][2]);
    }

    [TestMethod]
    public void Run_WhenValuesUseLiteralRows_ShouldRemainUnchanged()
    {
        const string query = """
                             from values {
                                 { Name: 'A', Score: 1 },
                                 { Name: 'B', Score: 2 }
                             } v
                             select v.Name, v.Score
                             order by v.Score
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource()).Run();

        Assert.AreEqual(2, table.Count);
        AssertColumn(table, 0, "v.Name", typeof(string));
        AssertColumn(table, 1, "v.Score", typeof(int));
        Assert.AreEqual("A", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("B", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
    }

    [TestMethod]
    public void Run_WhenPivotUsesMultipleMeasures_ShouldRemainUnchanged()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Jan' as Jan)
                             using Sum(Money) as Sales, Count(Name) as Items
                             group by City
                             """;

        var table = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { City = "GDA", Month = "Jan", Money = 10m, Name = "A" },
                new BasicEntity { City = "GDA", Month = "Jan", Money = 20m, Name = "B" }
            ]
        }).Run();

        Assert.AreEqual(1, table.Count);
        AssertColumn(table, 0, "City", typeof(string));
        AssertColumn(table, 1, "Jan_Sales", typeof(decimal?));
        AssertColumn(table, 2, "Jan_Items", typeof(long));
        Assert.AreEqual("GDA", table[0][0]);
        Assert.AreEqual(30m, table[0][1]);
        Assert.AreEqual(2L, table[0][2]);
    }

    [TestMethod]
    public void Run_WhenValuesUseNullAndNumericWidening_ShouldRemainUnchanged()
    {
        const string query = """
                             from values {
                                 { Name: 'A', Score: null },
                                 { Name: 'B', Score: 2l }
                             } v
                             select v.Name, v.Score
                             order by v.Name
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource()).Run();

        Assert.AreEqual(2, table.Count);
        AssertColumn(table, 0, "v.Name", typeof(string));
        AssertColumn(table, 1, "v.Score", typeof(long?));
        Assert.AreEqual("A", table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual("B", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
    }
}
