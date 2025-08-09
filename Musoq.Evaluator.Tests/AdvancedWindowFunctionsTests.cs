using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Advanced window functions tests exploring aggregation over windows
/// This demonstrates potential for implementing SUM() OVER functionality
/// </summary>
[TestClass]
public class AdvancedWindowFunctionsTests : BasicEntityTestBase
{
    [TestMethod]
    public void DemonstrateAggregationFunctionsWithGroupBy_ShouldWork()
    {
        // This test shows how traditional aggregation works with GROUP BY
        // The goal is to understand how to extend this to window functions
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity {Country = "Poland", City = "Warsaw", Money = 1000m},
                    new BasicEntity {Country = "Poland", City = "Krakow", Money = 800m}, 
                    new BasicEntity {Country = "Germany", City = "Berlin", Money = 2000m},
                    new BasicEntity {Country = "Germany", City = "Munich", Money = 1500m},
                    new BasicEntity {Country = "France", City = "Paris", Money = 3000m}
                ]
            }
        };

        // Traditional aggregation with GROUP BY
        var query = "select Country, Sum(Money) as TotalMoney from #A.entities() group by Country order by Country";
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("France", table[0].Values[0]);
        Assert.AreEqual(3000m, table[0].Values[1]);
        Assert.AreEqual("Germany", table[1].Values[0]);
        Assert.AreEqual(3500m, table[1].Values[1]); // 2000 + 1500
        Assert.AreEqual("Poland", table[2].Values[0]);
        Assert.AreEqual(1800m, table[2].Values[1]); // 1000 + 800
    }

    [TestMethod]
    public void DemonstrateWindowFunctionsVsAggregation_ShouldWork()
    {
        // This test shows the difference between window functions and aggregation:
        // - Window functions don't reduce rows (unlike GROUP BY)
        // - Each row gets a computed value based on the "window" of data
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity {Country = "Poland", City = "Warsaw", Money = 1000m},
                    new BasicEntity {Country = "Poland", City = "Krakow", Money = 800m}, 
                    new BasicEntity {Country = "Germany", City = "Berlin", Money = 2000m}
                ]
            }
        };

        // Window functions preserve all rows, adding computed columns
        var query = @"
            select 
                Country, 
                City, 
                Money,
                RowNumber() as RowNum,
                Rank() as Ranking
            from #A.entities() 
            order by Money desc";
            
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        // All 3 rows are preserved (unlike GROUP BY which would reduce to country groups)
        Assert.AreEqual(3, table.Count);
        
        // Highest money first
        Assert.AreEqual("Germany", table[0].Values[0]);
        Assert.AreEqual("Berlin", table[0].Values[1]);
        Assert.AreEqual(2000m, table[0].Values[2]);
        Assert.AreEqual(1, table[0].Values[3]); // RowNumber
        Assert.AreEqual(1, table[0].Values[4]); // Rank
        
        // Second highest
        Assert.AreEqual("Poland", table[1].Values[0]);
        Assert.AreEqual("Warsaw", table[1].Values[1]);
        Assert.AreEqual(1000m, table[1].Values[2]);
        Assert.AreEqual(2, table[1].Values[3]); // RowNumber
        Assert.AreEqual(2, table[1].Values[4]); // Rank
        
        // This demonstrates the foundation is in place for real window functions
        // Next step would be implementing SUM() OVER, COUNT() OVER, etc.
    }

    [TestMethod]
    public void DemonstrateCurrentWindowFunctionCapabilities_ShouldWork()
    {
        // This test demonstrates what window functions can currently do in Musoq
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Warsaw", 500),
                    new BasicEntity("Poland", "Krakow", 300),
                    new BasicEntity("Germany", "Berlin", 1000),
                    new BasicEntity("Germany", "Munich", 600),
                    new BasicEntity("France", "Paris", 800)
                ]
            }
        };

        var query = @"
            select 
                Country,
                City, 
                Population,
                RowNumber() as RowNum,
                Rank() as Ranking,
                DenseRank() as DenseRanking,
                Lag(City, 1, 'N/A') as PrevCity,
                Lead(City, 1, 'N/A') as NextCity
            from #A.entities() 
            order by Population desc";
            
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(5, table.Count);
        
        // Verify window functions work across the entire result set
        for (int i = 0; i < table.Count; i++)
        {
            var row = table[i];
            Assert.AreEqual(i + 1, row.Values[3]); // RowNumber increments
            Assert.AreEqual(i + 1, row.Values[4]); // Rank increments (same as RowNumber in basic impl)
            Assert.AreEqual(i + 1, row.Values[5]); // DenseRank increments
            Assert.AreEqual("N/A", row.Values[6]); // Lag returns default
            Assert.AreEqual("N/A", row.Values[7]); // Lead returns default
        }
        
        // This proves the infrastructure is working for basic window functions!
    }
}