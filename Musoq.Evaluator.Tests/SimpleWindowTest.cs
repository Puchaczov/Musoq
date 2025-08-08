using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SimpleWindowTest : BasicEntityTestBase
{
    [TestMethod]
    public void SimpleRankOver_ShouldWork()
    {
        var query = @"
            select 
                Population,
                RANK() OVER (ORDER BY Population) as PopRank
            from #A.entities() 
            order by Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 100, Country = "Poland" },
                    new BasicEntity { Population = 200, Country = "Germany" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Population", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("PopRank", table.Columns.ElementAt(1).ColumnName);
        
        // Basic validation that the function is callable
        Assert.IsTrue(table.Count > 0);
    }
}