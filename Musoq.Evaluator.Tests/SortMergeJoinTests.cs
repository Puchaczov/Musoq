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
    public class SortMergeJoinTests : BasicEntityTestBase
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
        public void LeftJoinNonEquiTest_SortMerge()
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

            // A1 (100) > B1 (50) -> Match
            // A2 (10) > B1 (50) -> No Match -> Left Join produces row with null B
            // Expected: 2 rows.

            var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: false, useSortMergeJoin: true));
            var table = vm.Run();

            Assert.AreEqual(2, table.Count, "SortMergeJoin Left Join failed");
            
            var rows = table.OrderBy(r => r[0]).ToList();
            Assert.AreEqual("A1", rows[0][0]);
            Assert.AreEqual("B1", rows[0][1]);
            
            Assert.AreEqual("A2", rows[1][0]);
            Assert.IsNull(rows[1][1]);
        }

        [TestMethod]
        public void RightJoinNonEquiTest_SortMerge()
        {
            var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
right join #B.entities() b on a.Population > b.Population";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity { Name = "A1", Population = 100 }
                    ]
                },
                {
                    "#B", [
                        new BasicEntity { Name = "B1", Population = 50 },
                        new BasicEntity { Name = "B2", Population = 200 }
                    ]
                }
            };

            // A1 (100) > B1 (50) -> Match
            // A1 (100) > B2 (200) -> False.
            // B2 is not matched by any A.
            // Right Join should produce:
            // 1. A1, B1
            // 2. null, B2
            
            var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: false, useSortMergeJoin: true));
            var table = vm.Run();

            Assert.AreEqual(2, table.Count, "SortMergeJoin Right Join failed");
            
            var rows = table.OrderBy(r => r[1]).ToList();
            Assert.AreEqual("A1", rows[0][0]);
            Assert.AreEqual("B1", rows[0][1]);
            
            Assert.IsNull(rows[1][0]);
            Assert.AreEqual("B2", rows[1][1]);
        }
    }
}
