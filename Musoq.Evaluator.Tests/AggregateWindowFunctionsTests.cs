using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AggregateWindowFunctionsTests : BasicEntityTestBase
{
    [TestMethod]
    public void SumOver_WithWindow_ShouldWork()
    {
        var query = @"
            select 
                Population,
                SUM(Population) OVER (ORDER BY Population) as RunningSum
            from #A.entities() 
            order by Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 100, Country = "Poland" },
                    new BasicEntity { Population = 200, Country = "Germany" },
                    new BasicEntity { Population = 300, Country = "Poland" },
                    new BasicEntity { Population = 400, Country = "Germany" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Population", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("RunningSum", table.Columns.ElementAt(1).ColumnName);
        
        // Basic validation that the function is callable
        Assert.IsTrue(table.Count > 0);
    }

    [TestMethod]
    public void CountOver_WithWindow_ShouldWork()
    {
        var query = @"
            select 
                Population,
                COUNT(Population) OVER (PARTITION BY Country) as CountByCountry
            from #A.entities() 
            order by Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 100, Country = "Poland" },
                    new BasicEntity { Population = 200, Country = "Germany" },
                    new BasicEntity { Population = 300, Country = "Poland" },
                    new BasicEntity { Population = 400, Country = "Germany" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Population", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("CountByCountry", table.Columns.ElementAt(1).ColumnName);
        
        // Basic validation that the function is callable
        Assert.IsTrue(table.Count > 0);
    }

    [TestMethod]
    public void AvgOver_WithWindow_ShouldWork()
    {
        var query = @"
            select 
                Population,
                AVG(Population) OVER (PARTITION BY Country ORDER BY Population) as AvgByCountry
            from #A.entities() 
            order by Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 100, Country = "Poland" },
                    new BasicEntity { Population = 200, Country = "Germany" },
                    new BasicEntity { Population = 300, Country = "Poland" },
                    new BasicEntity { Population = 400, Country = "Germany" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Population", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("AvgByCountry", table.Columns.ElementAt(1).ColumnName);
        
        // Basic validation that the function is callable
        Assert.IsTrue(table.Count > 0);
    }

    [TestMethod]
    public void MixedAggregateWindowFunctions_ShouldWork()
    {
        var query = @"
            select 
                Population,
                Country,
                SUM(Population) OVER (ORDER BY Population) as RunningSum,
                COUNT(Population) OVER (PARTITION BY Country) as CountByCountry,
                AVG(Population) OVER (PARTITION BY Country ORDER BY Population) as AvgByCountry,
                RANK() OVER (ORDER BY Population DESC) as Ranking
            from #A.entities() 
            order by Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 100, Country = "Poland" },
                    new BasicEntity { Population = 200, Country = "Germany" },
                    new BasicEntity { Population = 300, Country = "Poland" },
                    new BasicEntity { Population = 400, Country = "Germany" },
                    new BasicEntity { Population = 500, Country = "Poland" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(6, table.Columns.Count());
        Assert.AreEqual("Population", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("RunningSum", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual("CountByCountry", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual("AvgByCountry", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("Ranking", table.Columns.ElementAt(5).ColumnName);
        
        // Basic validation that the functions are callable together
        Assert.IsTrue(table.Count > 0);
    }
}