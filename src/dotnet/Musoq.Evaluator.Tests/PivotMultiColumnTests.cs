using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PivotMultiColumnTests : BasicEntityTestBase
{
    [TestMethod]
    public void Pivot_WithMultiColumnKeys_ShouldMatchTupleValues()
    {
        const string query = """
                             pivot #A.Entities()
                             on Id, Country in ((2000, 'NL') as y2000_nl, (2001, 'US') as y2001_us)
                             using Sum(Population) as Total
                             group by City
                             order by City
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run();

        Assert.AreEqual(2, table.Count);
        AssertColumn(table, 0, "City", typeof(string));
        AssertColumn(table, 1, "y2000_nl", typeof(decimal?));
        AssertColumn(table, 2, "y2001_us", typeof(decimal?));
        Assert.AreEqual("Amsterdam", table[0][0]);
        Assert.AreEqual(10m, table[0][1]);
        Assert.AreEqual(20m, table[0][2]);
        Assert.AreEqual("Rotterdam", table[1][0]);
        Assert.AreEqual(7m, table[1][1]);
        Assert.AreEqual(3m, table[1][2]);
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return CreateSingleSource(
            new BasicEntity { City = "Amsterdam", Id = 2000, Country = "NL", Population = 10m },
            new BasicEntity { City = "Amsterdam", Id = 2001, Country = "US", Population = 20m },
            new BasicEntity { City = "Amsterdam", Id = 2000, Country = "US", Population = 99m },
            new BasicEntity { City = "Rotterdam", Id = 2000, Country = "NL", Population = 7m },
            new BasicEntity { City = "Rotterdam", Id = 2001, Country = "US", Population = 3m });
    }
}
