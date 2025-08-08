using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class DebugCharacterAccessTests : BasicEntityTestBase
    {
        [TestMethod]
        public void DebugDirectCharacterAccess()
        {
            var query = @"select Name from #A.Entities() where Name[0] = 'd'";
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

            Console.WriteLine("=== Debug Direct Character Access ===");
            try
            {
                var vm = CreateAndRunVirtualMachine(query, sources);
                var table = vm.Run();
                Console.WriteLine($"Rows returned: {table.Count}");
                if (table.Count > 0)
                {
                    Console.WriteLine($"First row: {table[0].Values[0]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        [TestMethod]
        public void DebugAliasedCharacterAccess()
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

            Console.WriteLine("=== Debug Aliased Character Access ===");
            try
            {
                var vm = CreateAndRunVirtualMachine(query, sources);
                var table = vm.Run();
                Console.WriteLine($"Rows returned: {table.Count}");
                if (table.Count > 0)
                {
                    Console.WriteLine($"First row: {table[0].Values[0]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}