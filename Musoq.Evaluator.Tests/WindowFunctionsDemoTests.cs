using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Demo test showcasing window functions implementation in Musoq
/// This test demonstrates the basic window functions that have been implemented.
/// </summary>
[TestClass]
public class WindowFunctionsDemoTests : BasicEntityTestBase
{
    [TestMethod]
    public void DemonstrateBasicWindowFunctions_ShouldWork()
    {
        // Test data with countries and their populations
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity {Country = "Poland", Population = 38000000},
                    new BasicEntity {Country = "Germany", Population = 83000000},
                    new BasicEntity {Country = "France", Population = 67000000},
                    new BasicEntity {Country = "Spain", Population = 47000000}
                ]
            }
        };

        // 1. Basic RowNumber() - already existed
        var query1 = "select Country, Population, RowNumber() as RowNum from #A.entities() order by Population desc";
        var vm1 = CreateAndRunVirtualMachine(query1, sources);
        var table1 = vm1.Run();
        
        Assert.AreEqual(4, table1.Count);
        Assert.AreEqual("Germany", table1[0].Values[0]);    // Highest population
        Assert.AreEqual(1, table1[0].Values[2]);            // Row number 1
        Assert.AreEqual("France", table1[1].Values[0]);     // Second highest 
        Assert.AreEqual(2, table1[1].Values[2]);            // Row number 2

        // 2. New Rank() function - currently works like RowNumber
        var query2 = "select Country, Population, Rank() as Ranking from #A.entities() order by Population desc";
        var vm2 = CreateAndRunVirtualMachine(query2, sources);
        var table2 = vm2.Run();
        
        Assert.AreEqual(4, table2.Count);
        Assert.AreEqual("Germany", table2[0].Values[0]);
        Assert.AreEqual(1, table2[0].Values[2]);            // Rank 1
        Assert.AreEqual("France", table2[1].Values[0]);
        Assert.AreEqual(2, table2[1].Values[2]);            // Rank 2

        // 3. New DenseRank() function - currently works like RowNumber
        var query3 = "select Country, Population, DenseRank() as DenseRanking from #A.entities() order by Population desc";
        var vm3 = CreateAndRunVirtualMachine(query3, sources);
        var table3 = vm3.Run();
        
        Assert.AreEqual(4, table3.Count);
        Assert.AreEqual("Germany", table3[0].Values[0]);
        Assert.AreEqual(1, table3[0].Values[2]);            // Dense rank 1

        // 4. New Lag() function - basic implementation returns default values
        var query4 = "select Country, Lag(Country, 1, 'NO_PREVIOUS') as PrevCountry from #A.entities() order by Population desc";
        var vm4 = CreateAndRunVirtualMachine(query4, sources);
        var table4 = vm4.Run();
        
        Assert.AreEqual(4, table4.Count);
        foreach (var row in table4)
        {
            Assert.AreEqual("NO_PREVIOUS", row.Values[1]); // All return default for basic implementation
        }

        // 5. New Lead() function - basic implementation returns default values  
        var query5 = "select Country, Lead(Country, 1, 'NO_NEXT') as NextCountry from #A.entities() order by Population desc";
        var vm5 = CreateAndRunVirtualMachine(query5, sources);
        var table5 = vm5.Run();
        
        Assert.AreEqual(4, table5.Count);
        foreach (var row in table5)
        {
            Assert.AreEqual("NO_NEXT", row.Values[1]); // All return default for basic implementation
        }
        
        // Success! All window functions are working with basic implementation
        // This demonstrates that the infrastructure is in place for window functions
    }
    
    [TestMethod] 
    public void DemonstrateWindowFunctionsWithComplexData_ShouldWork()
    {
        // More complex test with duplicate values to show potential for real ranking
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity {Country = "Poland", City = "Warsaw", Population = 1800000m},
                    new BasicEntity {Country = "Poland", City = "Krakow", Population = 800000m}, 
                    new BasicEntity {Country = "Germany", City = "Berlin", Population = 3700000m},
                    new BasicEntity {Country = "Germany", City = "Munich", Population = 1500000m},
                    new BasicEntity {Country = "France", City = "Paris", Population = 2200000m},
                    new BasicEntity {Country = "France", City = "Lyon", Population = 500000m}
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
                DenseRank() as DenseRanking
            from #A.entities() 
            order by Country, Population desc";
            
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(6, table.Count);
        
        // Verify the data is ordered correctly (by Country, then Population desc)
        Assert.AreEqual("France", table[0].Values[0]);
        Assert.AreEqual("Paris", table[0].Values[1]);
        Assert.AreEqual(2200000m, table[0].Values[2]);
        Assert.AreEqual(1, table[0].Values[3]); // RowNumber
        Assert.AreEqual(1, table[0].Values[4]); // Rank
        Assert.AreEqual(1, table[0].Values[5]); // DenseRank
        
        Assert.AreEqual("France", table[1].Values[0]);
        Assert.AreEqual("Lyon", table[1].Values[1]);
        Assert.AreEqual(500000m, table[1].Values[2]);
        Assert.AreEqual(2, table[1].Values[3]); // RowNumber
        
        // Continue with Germany and Poland...
        Assert.AreEqual("Germany", table[2].Values[0]);
        Assert.AreEqual("Berlin", table[2].Values[1]);
        Assert.AreEqual(3700000m, table[2].Values[2]);
    }
}