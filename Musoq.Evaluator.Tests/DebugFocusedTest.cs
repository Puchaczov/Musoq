using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class DebugFocusedTest : BasicEntityTestBase
    {
        [TestMethod]
        public void TestAliasedBasicAccess()
        {
            // Test simple aliased column access (should work)
            var query1 = @"select Name from #A.Entities() f where f.Name = 'david.jones@proseware.com'";
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

            Console.WriteLine("=== Testing Aliased Basic Access ===");
            var vm1 = CreateAndRunVirtualMachine(query1, sources);
            var table1 = vm1.Run();
            Console.WriteLine($"Aliased basic access rows: {table1.Count}");
            Assert.AreEqual(1, table1.Count, "Basic aliased access should work");
        }

        [TestMethod] 
        public void TestDirectCharacterAccessInSelect()
        {
            // Test direct character access in SELECT (should work)
            var query = @"select Name[0] from #A.Entities() where Name = 'david.jones@proseware.com'";
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

            Console.WriteLine("=== Testing Direct Character Access in SELECT ===");
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            Console.WriteLine($"Direct character access in SELECT rows: {table.Count}");
            Assert.AreEqual(1, table.Count, "Direct character access in SELECT should work");
            if (table.Count > 0)
            {
                Assert.AreEqual("d", table[0].Values[0], "Should return first character");
            }
        }

        [TestMethod]
        public void TestAliasedCharacterAccessInSelect()
        {
            // Test aliased character access in SELECT (might work)
            var query = @"select f.Name[0] from #A.Entities() f where f.Name = 'david.jones@proseware.com'";
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

            Console.WriteLine("=== Testing Aliased Character Access in SELECT ===");
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            Console.WriteLine($"Aliased character access in SELECT rows: {table.Count}");
            Assert.AreEqual(1, table.Count, "Aliased character access in SELECT should work");
            if (table.Count > 0)
            {
                Assert.AreEqual("d", table[0].Values[0], "Should return first character");
            }
        }

        [TestMethod]
        public void TestDirectCharacterAccess()
        {
            // Test direct character access (should work)
            var query2 = @"select Name from #A.Entities() where Name[0] = 'd'";
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

            Console.WriteLine("=== Testing Direct Character Access ===");
            var vm2 = CreateAndRunVirtualMachine(query2, sources);
            var table2 = vm2.Run();
            Console.WriteLine($"Direct character access rows: {table2.Count}");
            Assert.AreEqual(1, table2.Count, "Direct character access should work");
        }

        [TestMethod]
        public void TestAliasedCharacterAccess()
        {
            // Test aliased character access (currently failing)
            var query3 = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
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

            Console.WriteLine("=== Testing Aliased Character Access ===");
            var vm3 = CreateAndRunVirtualMachine(query3, sources);
            var table3 = vm3.Run();
            Console.WriteLine($"Aliased character access rows: {table3.Count}");
            Assert.AreEqual(1, table3.Count, "Aliased character access should work");
        }
    }
}