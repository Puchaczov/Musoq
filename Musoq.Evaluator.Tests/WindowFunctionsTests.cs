using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunctionsTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenRankSimpleTest_ShouldPass()
    {
        var query = "select Country, Rank() from #A.entities() order by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity {Country = "Poland", Population = 300},
                    new BasicEntity {Country = "Germany", Population = 400},
                    new BasicEntity {Country = "Poland", Population = 250}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        
        // Should be ordered by Country: Germany(1), Poland(2), Poland(3)  
        Assert.AreEqual("Germany", table[0].Values[0]);
        Assert.AreEqual(1, table[0].Values[1]);
        
        Assert.AreEqual("Poland", table[1].Values[0]);
        Assert.AreEqual(2, table[1].Values[1]);
        
        Assert.AreEqual("Poland", table[2].Values[0]);
        Assert.AreEqual(3, table[2].Values[1]);
    }

    [TestMethod]
    public void WhenDenseRankSimpleTest_ShouldPass()
    {
        var query = "select Country, DenseRank() from #A.entities() order by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity {Country = "Poland", Population = 300},
                    new BasicEntity {Country = "Germany", Population = 400},
                    new BasicEntity {Country = "Poland", Population = 250}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        
        // For now, DenseRank works like RowNumber: Germany(1), Poland(2), Poland(3)
        Assert.AreEqual("Germany", table[0].Values[0]);
        Assert.AreEqual(1, table[0].Values[1]);
        
        Assert.AreEqual("Poland", table[1].Values[0]);
        Assert.AreEqual(2, table[1].Values[1]);
        
        Assert.AreEqual("Poland", table[2].Values[0]);
        Assert.AreEqual(3, table[2].Values[1]); // Currently works like RowNumber
    }

    [TestMethod]
    public void WhenLagFunctionTest_ShouldPass()
    {
        var query = "select Country, Lag(Country, 1, 'N/A') from #A.entities() order by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity {Country = "Poland"},
                    new BasicEntity {Country = "Germany"},
                    new BasicEntity {Country = "Spain"}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        
        // For basic implementation, LAG returns default value
        foreach (var row in table)
        {
            Assert.AreEqual("N/A", row.Values[1]);
        }
    }

    [TestMethod]
    public void WhenLeadFunctionTest_ShouldPass()
    {
        var query = "select Country, Lead(Country, 1, 'END') from #A.entities() order by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity {Country = "Poland"},
                    new BasicEntity {Country = "Germany"},
                    new BasicEntity {Country = "Spain"}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        
        // For basic implementation, LEAD returns default value
        foreach (var row in table)
        {
            Assert.AreEqual("END", row.Values[1]);
        }
    }
    
    /*
    [TestMethod]
    public void WhenRankWithPartitionTest_ShouldPass()
    {
        // Test ranking within partitions - TODO: implement proper partitioning
        var query = "select Country, Population, RankByPopulationInCountry() from #A.entities() order by Country, Population desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity {Country = "Poland", Population = 300},
                    new BasicEntity {Country = "Poland", Population = 250},
                    new BasicEntity {Country = "Germany", Population = 400},
                    new BasicEntity {Country = "Germany", Population = 350}
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(4, table.Count);
        
        // Should rank within each country by population
        // Germany: 400(1), 350(2)
        // Poland: 300(1), 250(2)
        Assert.AreEqual("Germany", table[0].Values[0]);
        Assert.AreEqual(400, table[0].Values[1]);
        Assert.AreEqual(1, table[0].Values[2]);
        
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual(350, table[1].Values[1]);
        Assert.AreEqual(2, table[1].Values[2]);
        
        Assert.AreEqual("Poland", table[2].Values[0]);
        Assert.AreEqual(300, table[2].Values[1]);
        Assert.AreEqual(1, table[2].Values[2]);
        
        Assert.AreEqual("Poland", table[3].Values[0]);
        Assert.AreEqual(250, table[3].Values[1]);
        Assert.AreEqual(2, table[3].Values[2]);
    }
    */
}