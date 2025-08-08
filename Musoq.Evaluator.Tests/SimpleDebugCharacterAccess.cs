using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class SimpleDebugCharacterAccess : BasicEntityTestBase
    {
        [TestMethod]
        public void DebugCharacterAccessSimple()
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("david.jones@proseware.com"),
                        new BasicEntity("other@example.com")
                    ]
                }
            };

            // Test 1: Direct access (works)
            try
            {
                var query1 = @"select Name from #A.Entities() where Name[0] = 'd'";
                var vm1 = CreateAndRunVirtualMachine(query1, sources);
                var table1 = vm1.Run();
                Assert.AreEqual(1, table1.Count, $"Direct access failed: expected 1 row, got {table1.Count}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Direct access threw exception: {ex.Message}");
            }

            // Test 2: Aliased access (currently broken)
            try
            {
                var query2 = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
                var vm2 = CreateAndRunVirtualMachine(query2, sources);
                var table2 = vm2.Run();
                Assert.AreEqual(1, table2.Count, $"Aliased access failed: expected 1 row, got {table2.Count}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Aliased access threw exception: {ex.Message}");
            }
        }
    }
}