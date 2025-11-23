using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Evaluator;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class DebugLeftJoinTests : BasicEntityTestBase
    {
        private CompiledQuery CreateAndRunVirtualMachine(
            string script,
            IDictionary<string, IEnumerable<BasicEntity>> sources,
            CompilationOptions options)
        {
            return InstanceCreator.CompileForExecution(
                script,
                Guid.NewGuid().ToString(),
                new BasicSchemaProvider<BasicEntity>(sources),
                LoggerResolver,
                options);
        }

        [TestMethod]
        public void LeftJoinNonEquiTest_Debug()
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
left join #B.entities() b on a.Population > b.Population";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Population = 100 },
                        new BasicEntity { Name = "A2", Population = 10 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Population = 50 }
                    ]
                }
            };

            // Try with default options (both enabled)
            var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions());
            var table = vm.Run();

            Assert.AreEqual(2, table.Count, "Default options failed");
        }

        [TestMethod]
        public void LeftJoinNonEquiTest_ForceSortMerge()
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
left join #B.entities() b on a.Population > b.Population";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Population = 100 },
                        new BasicEntity { Name = "A2", Population = 10 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Population = 50 }
                    ]
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: false, useSortMergeJoin: true));
            var table = vm.Run();

            Assert.AreEqual(2, table.Count, "SortMergeJoin failed");
        }

        [TestMethod]
        public void LeftJoinNonEquiTest_DisableAllOptimizations()
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
left join #B.entities() b on a.Population > b.Population";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Population = 100 },
                        new BasicEntity { Name = "A2", Population = 10 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Population = 50 }
                    ]
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));
            var table = vm.Run();

            Assert.AreEqual(2, table.Count, "No optimizations failed");
        }
    }
}
