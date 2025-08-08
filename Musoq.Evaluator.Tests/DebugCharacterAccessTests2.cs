using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class DebugCharacterAccessTests2 : BasicEntityTestBase
    {
        [TestMethod]
        public void DebugAliasedCharacterAccessDetailed()
        {
            var query = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("12@hostname.com"),
                        new BasicEntity("ma@hostname.comcom"),
                        new BasicEntity("david.jones@proseware.com"),
                        new BasicEntity("ma@hostname.com")
                    ]
                }
            };

            try
            {
                var vm = CreateAndRunVirtualMachine(query, sources);
                var table = vm.Run();
                
                // This should return 1 row with "david.jones@proseware.com" but returns 0 rows
                Assert.AreEqual(1, table.Count, $"Expected 1 row but got {table.Count}");
                if (table.Count > 0)
                {
                    Assert.AreEqual("david.jones@proseware.com", table[0].Values[0]);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception occurred: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }
        
        [TestMethod]
        public void DebugBothCharacterAccessComparison()
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("12@hostname.com"),
                        new BasicEntity("ma@hostname.comcom"),
                        new BasicEntity("david.jones@proseware.com"),
                        new BasicEntity("ma@hostname.com")
                    ]
                }
            };

            // Direct access - should work
            var query1 = @"select Name from #A.Entities() where Name[0] = 'd'";
            var vm1 = CreateAndRunVirtualMachine(query1, sources);
            var table1 = vm1.Run();
            
            // Aliased access - should work but doesn't
            var query2 = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
            var vm2 = CreateAndRunVirtualMachine(query2, sources);
            var table2 = vm2.Run();
            
            // Both should return the same result
            Assert.AreEqual(1, table1.Count, "Direct access should return 1 row");
            Assert.AreEqual(1, table2.Count, "Aliased access should return 1 row");
            
            if (table1.Count > 0 && table2.Count > 0)
            {
                Assert.AreEqual(table1[0].Values[0], table2[0].Values[0], "Both queries should return the same value");
            }
        }
    }
}