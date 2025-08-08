using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class DebugCharacterIndexTests : BasicEntityTestBase
{
    [TestMethod]
    public void TestAliasedCharacterAccessDebug()
    {
        // Test the failing case with debugging info
        var query = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                new[]
                {
                    new BasicEntity("david.jones@proseware.com") // This should match
                }
            }
        };

        Console.WriteLine("=== DEBUGGING ALIASED CHARACTER ACCESS ===");
        Console.WriteLine($"Query: {query}");
        Console.WriteLine($"Expected result: 1 row with 'david.jones@proseware.com'");
        Console.WriteLine($"First character of test string: '{sources["#A"].First().Name[0]}'");
        Console.WriteLine($"Character comparison: '{sources["#A"].First().Name[0]}' == 'd' -> {sources["#A"].First().Name[0] == 'd'}");
        
        try
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            
            Console.WriteLine($"Actual result: {table.Count} rows");
            for (int i = 0; i < table.Count; i++)
            {
                Console.WriteLine($"Row {i}: {table[i].Values[0]}");
            }
            
            // Log the column info
            Console.WriteLine($"Columns: {table.Columns.Count()}");
            foreach (var column in table.Columns)
            {
                Console.WriteLine($"Column: {column.ColumnName} ({column.ColumnType})");
            }
            
            // This should pass but currently fails
            if (table.Count == 0)
            {
                Console.WriteLine("ERROR: No rows returned - the WHERE condition f.Name[0] = 'd' is not working");
                Console.WriteLine("This suggests the character access f.Name[0] is not generating the correct value");
            }
            
            Assert.AreEqual(1, table.Count, "Should return 1 row with matching character");
            Assert.AreEqual("david.jones@proseware.com", table[0].Values[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
    
    [TestMethod]
    public void CompareDirectVsAliasedAccess()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                new[]
                {
                    new BasicEntity("david.jones@proseware.com")
                }
            }
        };

        Console.WriteLine("=== COMPARING DIRECT VS ALIASED ACCESS ===");
        
        // Test direct access (this works)
        var directQuery = @"select Name from #A.Entities() where Name[0] = 'd'";
        Console.WriteLine($"Direct query: {directQuery}");
        
        var vm1 = CreateAndRunVirtualMachine(directQuery, sources);
        var table1 = vm1.Run();
        Console.WriteLine($"Direct result: {table1.Count} rows");
        
        // Test aliased access (this fails)
        var aliasedQuery = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
        Console.WriteLine($"Aliased query: {aliasedQuery}");
        
        var vm2 = CreateAndRunVirtualMachine(aliasedQuery, sources);
        var table2 = vm2.Run();
        Console.WriteLine($"Aliased result: {table2.Count} rows");
        
        // Both should return the same result
        Console.WriteLine($"Results match: {table1.Count == table2.Count}");
    }
}