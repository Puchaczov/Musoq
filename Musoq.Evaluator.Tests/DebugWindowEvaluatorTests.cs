using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class DebugWindowEvaluatorTests : BasicEntityTestBase
    {
        [TestMethod]
        public void Debug_RankWithOver_ShouldWork()
        {
            var query = "select Country, Rank() OVER () from #A.entities() order by Country";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity {Country = "Poland", Population = 300},
                        new BasicEntity {Country = "Germany", Population = 400}
                    ]
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            
            Assert.AreEqual(2, table.Count);
        }
    }
}