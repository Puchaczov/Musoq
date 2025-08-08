using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass] 
    public class DebugAliasTest : BasicEntityTestBase
    {
        [TestMethod]
        public void DebugAliasedCharacterAccessSimple()
        {
            var query = @"select f.Name[0] from #A.Entities() f";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("david.jones@proseware.com"),
                        new BasicEntity("test")
                    ]
                }
            };

            try
            {
                var vm = CreateAndRunVirtualMachine(query, sources);
                var table = vm.Run();

                Console.WriteLine($"Results: {table.Count} rows");
                foreach (var row in table)
                {
                    Console.WriteLine($"Row: '{row.Values[0]}' (Length: {row.Values[0].ToString().Length})");
                }

                Assert.IsTrue(table.Count >= 1, "Should return at least 1 row");
                
                // Let's verify what we're actually getting
                var firstResult = table[0].Values[0].ToString();
                Console.WriteLine($"First result type: {table[0].Values[0].GetType()}");
                Console.WriteLine($"First result value: '{firstResult}'");
                
                // Check if we're getting individual characters or full strings
                if (firstResult.Length == 1)
                {
                    Console.WriteLine("✅ Getting individual characters as expected");
                }
                else
                {
                    Console.WriteLine($"❌ Getting full strings ({firstResult.Length} chars) instead of characters");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        [TestMethod]
        public void DebugDirectCharacterAccessSimple()
        {
            var query = @"select Name[0] from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("david.jones@proseware.com"),
                        new BasicEntity("test")
                    ]
                }
            };

            try
            {
                var vm = CreateAndRunVirtualMachine(query, sources);
                var table = vm.Run();

                Console.WriteLine($"Results: {table.Count} rows");
                foreach (var row in table)
                {
                    Console.WriteLine($"Row: {row.Values[0]}");
                }

                Assert.IsTrue(table.Count >= 1, "Should return at least 1 row");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        [TestMethod]
        public void DebugAliasedCharacterWhereClause()
        {
            var query = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("david.jones@proseware.com"),
                        new BasicEntity("test"),
                        new BasicEntity("dog")
                    ]
                }
            };

            try
            {
                var vm = CreateAndRunVirtualMachine(query, sources);
                var table = vm.Run();

                Console.WriteLine($"Results: {table.Count} rows");
                foreach (var row in table)
                {
                    Console.WriteLine($"Row: {row.Values[0]}");
                }

                Assert.IsTrue(table.Count >= 1, "Should return at least 1 row");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}