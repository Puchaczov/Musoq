using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class MethodInvocationTests : BasicEntityTestBase
{
    [TestMethod]
    public void MethodInvocationOnAliasFinishedWithNumber_ShouldPass()
    {
        var query = "select population2.GetPopulation() from #A.entities() population2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", new[]
                {
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                }
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(500m, table[0].Values[0]);
        Assert.AreEqual(400m, table[1].Values[0]);
        Assert.AreEqual(250m, table[2].Values[0]);
        Assert.AreEqual(250m, table[3].Values[0]);
        Assert.AreEqual(350m, table[4].Values[0]);
    }
}