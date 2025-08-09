using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SimpleWindowFunctionDebugTests : BasicEntityTestBase
{
    [TestMethod]
    public void SingleSumWindowFunction_ShouldWork()
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
                    new BasicEntity { Population = 200, Country = "Germany" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.IsTrue(table.Count > 0);
    }
}